using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Shadows
{
    public class ToonVsmShadows
    {
        private const string CmdName = "Shadows";
        private const string BlurCmdName = "Blur";

        private const int MaxShadowedDirectionalLightCount = 1;
        private const int DepthBits = 32;
        public const int MaxCascades = 4;

        // R - depth, G - depth^2
        private const RenderTextureFormat VsmShadowmapFormat = RenderTextureFormat.RGFloat;
        private const FilterMode ShadowmapFiltering = FilterMode.Bilinear;

        private static readonly int DirectionalShadowsAtlasId = Shader.PropertyToID("_ToonRP_DirectionalShadowAtlas");
        private static readonly int DirectionalShadowsAtlasTempId =
            Shader.PropertyToID("_ToonRP_DirectionalShadowAtlas_Temp");
        private static readonly int DirectionalShadowsMatricesVpId =
            Shader.PropertyToID("_ToonRP_DirectionalShadowMatrices_VP");
        private static readonly int DirectionalShadowsMatricesVId =
            Shader.PropertyToID("_ToonRP_DirectionalShadowMatrices_V");
        private static readonly int CascadeCountId =
            Shader.PropertyToID("_ToonRP_CascadeCount");
        private static readonly int CascadeCullingSpheresId =
            Shader.PropertyToID("_ToonRP_CascadeCullingSpheres");
        private static readonly int ShadowBiasId =
            Shader.PropertyToID("_ToonRP_ShadowBias");
        private readonly CommandBuffer _blurCmd = new() { name = BlurCmdName };
        private readonly Vector4[] _cascadeCullingSpheres = new Vector4[MaxCascades];

        private readonly CommandBuffer _cmd = new() { name = CmdName };
        private readonly Matrix4x4[] _directionalShadowMatricesV =
            new Matrix4x4[MaxShadowedDirectionalLightCount * MaxCascades];
        private readonly Matrix4x4[] _directionalShadowMatricesVp =
            new Matrix4x4[MaxShadowedDirectionalLightCount * MaxCascades];

        private readonly ShadowedDirectionalLight[] _shadowedDirectionalLights =
            new ShadowedDirectionalLight[MaxShadowedDirectionalLightCount];
        private Material _blurMaterial;
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;
        private LocalKeyword _highQualityBlurKeyword;
        private ToonShadowSettings _settings;
        private int _shadowedDirectionalLightCount;

        private ToonVsmShadowSettings _vsmSettings;

        private void EnsureMaterialIsCreated()
        {
            if (_blurMaterial != null)
            {
                return;
            }

            var blurShader = Shader.Find("Hidden/Toon RP/VSM Blur");
            _blurMaterial = new Material(blurShader)
            {
                name = "Toon RP VSM Blur",
            };
            _highQualityBlurKeyword = new LocalKeyword(blurShader, "_TOON_RP_VSM_BLUR_HIGH_QUALITY");
        }


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
            _vsmSettings = settings.Vsm;
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
                    NearPlaneOffset = light.shadowNearPlane,
                };
            }
        }

        public void Render()
        {
            if (_vsmSettings.Directional.Enabled && _shadowedDirectionalLightCount > 0)
            {
                bool useCascades = _vsmSettings.Directional.CascadeCount > 1;
                _cmd.SetKeyword(ToonShadows.DirectionalShadowsGlobalKeyword, !useCascades);
                _cmd.SetKeyword(ToonShadows.DirectionalCascadedShadowsGlobalKeyword, useCascades);
                _cmd.SetKeyword(ToonShadows.VsmGlobalKeyword, _vsmSettings.Blur != ToonVsmShadowSettings.BlurMode.None);
                _cmd.SetKeyword(ToonShadows.ShadowsRampCrisp, _settings.CrispAntiAliased);

                EnsureMaterialIsCreated();
                RenderDirectionalShadows();
            }
            else
            {
                _cmd.GetTemporaryRT(DirectionalShadowsAtlasId, 1, 1,
                    DepthBits,
                    ShadowmapFiltering,
                    RenderTextureFormat.Shadowmap
                );
                _cmd.DisableKeyword(ToonShadows.DirectionalShadowsGlobalKeyword);
                _cmd.DisableKeyword(ToonShadows.DirectionalCascadedShadowsGlobalKeyword);
                _cmd.DisableKeyword(ToonShadows.VsmGlobalKeyword);
                _cmd.DisableKeyword(ToonShadows.ShadowsRampCrisp);
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
            int atlasSize = (int) _vsmSettings.Directional.AtlasSize;


            if (_vsmSettings.Blur != ToonVsmShadowSettings.BlurMode.None)
            {
                _cmd.GetTemporaryRT(DirectionalShadowsAtlasId, atlasSize, atlasSize,
                    DepthBits,
                    ShadowmapFiltering,
                    VsmShadowmapFormat
                );
                _cmd.GetTemporaryRT(DirectionalShadowsAtlasTempId, atlasSize, atlasSize,
                    0,
                    ShadowmapFiltering,
                    VsmShadowmapFormat
                );
            }
            else
            {
                _cmd.GetTemporaryRT(DirectionalShadowsAtlasId, atlasSize, atlasSize, DepthBits, ShadowmapFiltering,
                    RenderTextureFormat.Shadowmap
                );
            }

            _cmd.SetRenderTarget(DirectionalShadowsAtlasId,
                RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store
            );
            _cmd.ClearRenderTarget(true, true, GetShadowmapClearColor());
            _cmd.BeginSample(CmdName);
            ExecuteBuffer();

            int tiles = _shadowedDirectionalLightCount * _vsmSettings.Directional.CascadeCount;
            int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            int tileSize = atlasSize / split;

            for (int i = 0; i < _shadowedDirectionalLightCount; ++i)
            {
                RenderDirectionalShadows(i, split, tileSize);
            }

            _cmd.SetGlobalInteger(CascadeCountId, _vsmSettings.Directional.CascadeCount);
            _cmd.SetGlobalVectorArray(CascadeCullingSpheresId, _cascadeCullingSpheres);
            _cmd.SetGlobalMatrixArray(DirectionalShadowsMatricesVpId, _directionalShadowMatricesVp);
            _cmd.SetGlobalMatrixArray(DirectionalShadowsMatricesVId, _directionalShadowMatricesV);
            _cmd.EndSample(CmdName);
            ExecuteBuffer();
        }

        private static Color GetShadowmapClearColor()
        {
            var color = new Color(Mathf.NegativeInfinity, Mathf.Infinity, 0.0f, 0.0f);

            if (SystemInfo.usesReversedZBuffer)
            {
                color.r *= -1;
            }

            return color;
        }

        private void RenderDirectionalShadows(int index, int split, int tileSize)
        {
            ShadowedDirectionalLight light = _shadowedDirectionalLights[index];
            var shadowSettings = new ShadowDrawingSettings(_cullingResults, light.VisibleLightIndex);
            int cascadeCount = _vsmSettings.Directional.CascadeCount;
            int tileOffset = index * cascadeCount;
            Vector3 ratios = _vsmSettings.Directional.GetRatios();

            _cmd.SetGlobalDepthBias(0.0f, _vsmSettings.Directional.SlopeBias);
            _cmd.SetGlobalVector(ShadowBiasId,
                new Vector4(-_vsmSettings.Directional.DepthBias, _vsmSettings.Directional.NormalBias)
            );

            for (int i = 0; i < cascadeCount; i++)
            {
                _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                    light.VisibleLightIndex, i, cascadeCount, ratios, tileSize, light.NearPlaneOffset,
                    out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData
                );
                shadowSettings.splitData = splitData;
                if (index == 0)
                {
                    Vector4 cullingSphere = splitData.cullingSphere;
                    cullingSphere.w *= cullingSphere.w;
                    _cascadeCullingSpheres[i] = cullingSphere;
                }

                int tileIndex = tileOffset + i;
                SetTileViewport(tileIndex, split, tileSize, out Vector2 offset);
                _directionalShadowMatricesVp[tileIndex] =
                    ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, split);
                _directionalShadowMatricesV[tileIndex] = viewMatrix;
                _cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

                ExecuteBuffer();

                _context.DrawShadows(ref shadowSettings);
            }

            _cmd.SetGlobalDepthBias(0f, 0.0f);
            ExecuteBuffer();

            // TODO: try using _blurCmd.SetKeyword
            if (_vsmSettings.Blur != ToonVsmShadowSettings.BlurMode.None)
            {
                bool highQualityBlur = _vsmSettings.Blur == ToonVsmShadowSettings.BlurMode.HighQuality;
                _blurMaterial.SetKeyword(_highQualityBlurKeyword, highQualityBlur);
                _blurCmd.Blit(DirectionalShadowsAtlasId, DirectionalShadowsAtlasTempId, _blurMaterial, 0);
                _blurCmd.Blit(DirectionalShadowsAtlasTempId, DirectionalShadowsAtlasId, _blurMaterial, 1);
            }

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

        private Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
        {
            Matrix4x4 remap = Matrix4x4.identity;
            remap.m00 = remap.m11 = 0.5f; // scale [-1; 1] -> [-0.5, 0.5]
            remap.m03 = remap.m13 = 0.5f; // translate [-0.5, 0.5] -> [0, 1]

            if (_vsmSettings.Blur == ToonVsmShadowSettings.BlurMode.None)
            {
                remap.m22 = 0.5f; // scale [-1; 1] -> [-0.5, 0.5]

                if (SystemInfo.usesReversedZBuffer)
                {
                    remap.m22 *= -1;
                }

                remap.m23 = 0.5f; // translate [-0.5, 0.5] -> [0, 1]
            }

            m = remap * m;

            float scale = 1f / split;
            remap = Matrix4x4.identity;
            remap.m00 = remap.m11 = scale;
            remap.m03 = offset.x * scale;
            remap.m13 = offset.y * scale;

            m = remap * m;

            return m;
        }

        private struct ShadowedDirectionalLight
        {
            public int VisibleLightIndex;
            public float NearPlaneOffset;
        }
    }
}