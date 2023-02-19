using UnityEngine;
using UnityEngine.Rendering;

namespace ToonRP.Runtime
{
    public class ToonShadows
    {
        private const string CmdName = "Shadows";
        private const string BlurCmdName = "Blur";

        private const int MaxShadowedDirectionalLightCount = 1;
        private const int DepthBits = 32;

        // R - depth, G - depth^2
        private const RenderTextureFormat ShadowmapFormat = RenderTextureFormat.RGFloat;
        private const FilterMode ShadowmapFiltering = FilterMode.Bilinear;

        private static readonly int DirectionalShadowsAtlasId = Shader.PropertyToID("_ToonRP_DirectionalShadowAtlas");
        private static readonly int DirectionalShadowsAtlasTempId =
            Shader.PropertyToID("_ToonRP_DirectionalShadowAtlas_Temp");
        private static readonly int DirectionalShadowsMatricesVpId =
            Shader.PropertyToID("_ToonRP_DirectionalShadowMatrices_VP");
        private static readonly int DirectionalShadowsMatricesVId =
            Shader.PropertyToID("_ToonRP_DirectionalShadowMatrices_V");
        private static readonly int ShadowRampId =
            Shader.PropertyToID("_ToonRP_ShadowRamp");
        private static readonly int ShadowDistanceFadeId =
            Shader.PropertyToID("_ToonRP_ShadowDistanceFade");
        private readonly CommandBuffer _blurCmd = new() { name = BlurCmdName };

        private readonly Material _blurMaterial = new(Shader.Find("Hidden/Toon RP/VSM Gaussian Blur"));

        private readonly CommandBuffer _cmd = new() { name = CmdName };
        private readonly Matrix4x4[] _directionalShadowMatricesV =
            new Matrix4x4[MaxShadowedDirectionalLightCount];
        private readonly Matrix4x4[] _directionalShadowMatricesVp =
            new Matrix4x4[MaxShadowedDirectionalLightCount];

        private readonly ShadowedDirectionalLight[] _shadowedDirectionalLights =
            new ShadowedDirectionalLight[MaxShadowedDirectionalLightCount];
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;

        private ToonShadowSettings _settings;
        private int _shadowedDirectionalLightCount;


        private void ExecuteBuffer(CommandBuffer commandBuffer)
        {
            _context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }

        private void ExecuteBuffer() => ExecuteBuffer(_cmd);

        public void Setup(in ScriptableRenderContext context,
            in CullingResults cullingResults,
            in ToonShadowSettings settings)
        {
            _context = context;
            _cullingResults = cullingResults;
            _settings = settings;
            _shadowedDirectionalLightCount = 0;
        }

        public void ReserveDirectionalShadows(Light light, int visibleLightIndex)
        {
            if (_shadowedDirectionalLightCount < MaxShadowedDirectionalLightCount &&
                light.shadows != LightShadows.None && light.shadowStrength > 0f &&
                _cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds _)
               )
            {
                _shadowedDirectionalLights[_shadowedDirectionalLightCount++] = new ShadowedDirectionalLight
                {
                    VisibleLightIndex = visibleLightIndex,
                };
            }
        }

        public void Render()
        {
            if (_shadowedDirectionalLightCount > 0)
            {
                RenderDirectionalShadows();
            }
            else
            {
                _cmd.GetTemporaryRT(DirectionalShadowsAtlasId, 1, 1,
                    DepthBits,
                    ShadowmapFiltering,
                    ShadowmapFormat
                );
            }

            ExecuteBuffer();
        }

        public void Cleanup()
        {
            _cmd.ReleaseTemporaryRT(DirectionalShadowsAtlasId);
            _cmd.ReleaseTemporaryRT(DirectionalShadowsAtlasTempId);
            ExecuteBuffer();
        }

        private void RenderDirectionalShadows()
        {
            int atlasSize = (int) _settings.Directional.AtlasSize;
            _cmd.GetTemporaryRT(DirectionalShadowsAtlasId, atlasSize, atlasSize,
                DepthBits,
                ShadowmapFiltering,
                ShadowmapFormat
            );
            _cmd.GetTemporaryRT(DirectionalShadowsAtlasTempId, atlasSize, atlasSize,
                0,
                ShadowmapFiltering,
                ShadowmapFormat
            );
            _cmd.SetRenderTarget(DirectionalShadowsAtlasId,
                RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store
            );
            _cmd.ClearRenderTarget(true, true, new Color(Mathf.Infinity, Mathf.Infinity, 0.0f, 0.0f));
            _cmd.BeginSample(CmdName);
            ExecuteBuffer();

            int split = _shadowedDirectionalLightCount <= 1 ? 1 : 2;
            int tileSize = atlasSize / split;

            for (int i = 0; i < _shadowedDirectionalLightCount; ++i)
            {
                RenderDirectionalShadows(i, split, tileSize);
            }

            _cmd.SetGlobalMatrixArray(DirectionalShadowsMatricesVpId, _directionalShadowMatricesVp);
            _cmd.SetGlobalMatrixArray(DirectionalShadowsMatricesVId, _directionalShadowMatricesV);
            _cmd.SetGlobalVector(ShadowRampId,
                new Vector4(
                    _settings.Directional.Threshold,
                    _settings.Directional.Threshold + _settings.Directional.Smoothness
                )
            );
            _cmd.SetGlobalVector(ShadowDistanceFadeId,
                new Vector4(
                    1.0f / _settings.MaxDistance,
                    1.0f / _settings.DistanceFade
                )
            );
            _cmd.EndSample(CmdName);
            ExecuteBuffer();
        }

        private void RenderDirectionalShadows(int index, int split, int tileSize)
        {
            ShadowedDirectionalLight light = _shadowedDirectionalLights[index];
            var shadowSettings = new ShadowDrawingSettings(_cullingResults, light.VisibleLightIndex);
            _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                light.VisibleLightIndex, index, 1, Vector3.zero, tileSize, 0f,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData
            );
            shadowSettings.splitData = splitData;
            SetTileViewport(index, split, tileSize, out Vector2 offset);
            _directionalShadowMatricesVp[index] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileSize);
            _directionalShadowMatricesV[index] = viewMatrix;
            _cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

            ExecuteBuffer();
            _context.DrawShadows(ref shadowSettings);

            _blurCmd.Blit(DirectionalShadowsAtlasId, DirectionalShadowsAtlasTempId, _blurMaterial, 0);
            _blurCmd.Blit(DirectionalShadowsAtlasTempId, DirectionalShadowsAtlasId, _blurMaterial, 1);
            ExecuteBuffer(_blurCmd);
        }

        private void SetTileViewport(int index, int split, int tileSize, out Vector2 offset)
        {
            // ReSharper disable once PossibleLossOfFraction
            offset = new Vector2(index % split, index / split);
            _cmd.SetViewport(new Rect(
                    offset.x * tileSize,
                    offset.y * tileSize,
                    tileSize,
                    tileSize
                )
            );
        }

        private static Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split) =>
            // TODO: construct a proper matrix to avoid all the calculations on GPU
            m;

        private struct ShadowedDirectionalLight
        {
            public int VisibleLightIndex;
        }
    }
}