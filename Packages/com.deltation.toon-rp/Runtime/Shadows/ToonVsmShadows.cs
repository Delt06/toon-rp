using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Shadows
{
    public class ToonVsmShadows
    {
        private const string RenderShadowsSample = "Render Shadows";
        private const string BlurSample = "Blur";

        private const int MaxShadowedDirectionalLightCount = 1;
        private const int DepthBits = 32;
        public const int MaxCascades = 4;

        // ShouldMirrorTheValue in VSM.hlsl
        private const float DepthScale = 0.1f;

        // R - depth, G - depth^2
        private const RenderTextureFormat VsmShadowmapFormat = RenderTextureFormat.RGFloat;
        private const FilterMode ShadowmapFiltering = FilterMode.Bilinear;

        public const string BlurShaderName = "Hidden/Toon RP/VSM Blur";
        public const string BlurHighQualityKeywordName = "_TOON_RP_VSM_BLUR_HIGH_QUALITY";
        public const string BlurEarlyBailKeywordName = "_TOON_RP_VSM_BLUR_EARLY_BAIL";
        private const RenderTextureFormat VsmShadowmapDepthFormat = RenderTextureFormat.Shadowmap;

        private static readonly string[] CascadeProfilingNames;

        private static readonly int DirectionalShadowsAtlasId = Shader.PropertyToID("_ToonRP_DirectionalShadowAtlas");
        private static readonly int DirectionalShadowsAtlasDepthId =
            Shader.PropertyToID("_ToonRP_DirectionalShadowAtlas_Depth");
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
        private static readonly int EarlyBailThresholdId = Shader.PropertyToID("_EarlyBailThreshold");
        private readonly Vector4[] _cascadeCullingSpheres = new Vector4[MaxCascades];

        private readonly Matrix4x4[] _directionalShadowMatricesV =
            new Matrix4x4[MaxShadowedDirectionalLightCount * MaxCascades];
        private readonly Matrix4x4[] _directionalShadowMatricesVp =
            new Matrix4x4[MaxShadowedDirectionalLightCount * MaxCascades];

        private readonly ShadowedDirectionalLight[] _shadowedDirectionalLights =
            new ShadowedDirectionalLight[MaxShadowedDirectionalLightCount];
        private Material _blurMaterial;
        private Shader _blurShader;
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;
        private ToonShadowSettings _settings;
        private int _shadowedDirectionalLightCount;

        private ToonVsmShadowSettings _vsmSettings;

        static ToonVsmShadows()
        {
            CascadeProfilingNames = new string[MaxCascades];

            for (int i = 0; i < MaxCascades; i++)
            {
                CascadeProfilingNames[i] = $"Cascade {i}";
            }
        }

        private void EnsureMaterialIsCreated()
        {
            if (_blurMaterial != null && _blurShader != null)
            {
                return;
            }

            _blurShader = Shader.Find(BlurShaderName);
            _blurMaterial = new Material(_blurShader)
            {
                name = "Toon RP VSM Blur",
            };
        }


        private void ExecuteBuffer(CommandBuffer cmd)
        {
            _context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

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
            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.VsmShadows)))
            {
                if (_vsmSettings.Directional.Enabled && _shadowedDirectionalLightCount > 0)
                {
                    bool useCascades = _vsmSettings.Directional.CascadeCount > 1;
                    cmd.SetKeyword(ToonShadows.DirectionalShadowsGlobalKeyword, !useCascades);
                    cmd.SetKeyword(ToonShadows.DirectionalCascadedShadowsGlobalKeyword, useCascades);
                    cmd.SetKeyword(ToonShadows.VsmGlobalKeyword,
                        _vsmSettings.Blur != ToonVsmShadowSettings.BlurMode.None
                    );
                    cmd.SetKeyword(ToonShadows.ShadowsRampCrisp, _settings.CrispAntiAliased);

                    EnsureMaterialIsCreated();
                    RenderDirectionalShadows(cmd);
                }
                else
                {
                    cmd.GetTemporaryRT(DirectionalShadowsAtlasId, 1, 1,
                        DepthBits,
                        ShadowmapFiltering,
                        VsmShadowmapDepthFormat
                    );
                    cmd.DisableKeyword(ToonShadows.DirectionalShadowsGlobalKeyword);
                    cmd.DisableKeyword(ToonShadows.DirectionalCascadedShadowsGlobalKeyword);
                    cmd.DisableKeyword(ToonShadows.VsmGlobalKeyword);
                    cmd.DisableKeyword(ToonShadows.ShadowsRampCrisp);
                }
            }

            ExecuteBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Cleanup()
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.ReleaseTemporaryRT(DirectionalShadowsAtlasId);
            cmd.ReleaseTemporaryRT(DirectionalShadowsAtlasDepthId);
            cmd.ReleaseTemporaryRT(DirectionalShadowsAtlasTempId);
            ExecuteBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void RenderDirectionalShadows(CommandBuffer cmd)
        {
            int atlasSize = (int) _vsmSettings.Directional.AtlasSize;


            using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Prepare Shadowmaps")))
            {
                if (_vsmSettings.Blur != ToonVsmShadowSettings.BlurMode.None)
                {
                    cmd.GetTemporaryRT(DirectionalShadowsAtlasId, atlasSize, atlasSize,
                        0,
                        ShadowmapFiltering,
                        VsmShadowmapFormat
                    );
                    cmd.GetTemporaryRT(DirectionalShadowsAtlasDepthId, atlasSize, atlasSize,
                        DepthBits,
                        ShadowmapFiltering,
                        VsmShadowmapDepthFormat
                    );
                    cmd.GetTemporaryRT(DirectionalShadowsAtlasTempId, atlasSize, atlasSize,
                        0,
                        ShadowmapFiltering,
                        VsmShadowmapFormat
                    );
                    cmd.SetRenderTarget(DirectionalShadowsAtlasId,
                        RenderBufferLoadAction.DontCare,
                        RenderBufferStoreAction.Store,
                        DirectionalShadowsAtlasDepthId,
                        RenderBufferLoadAction.DontCare,
                        RenderBufferStoreAction.Store
                    );
                }
                else
                {
                    cmd.GetTemporaryRT(DirectionalShadowsAtlasId, atlasSize, atlasSize, DepthBits, ShadowmapFiltering,
                        VsmShadowmapDepthFormat
                    );
                    cmd.SetRenderTarget(DirectionalShadowsAtlasId,
                        RenderBufferLoadAction.DontCare,
                        RenderBufferStoreAction.Store
                    );
                }


                cmd.ClearRenderTarget(true, true, GetShadowmapClearColor());
            }

            ExecuteBuffer(cmd);

            int tiles = _shadowedDirectionalLightCount * _vsmSettings.Directional.CascadeCount;
            int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            int tileSize = atlasSize / split;

            for (int i = 0; i < _shadowedDirectionalLightCount; ++i)
            {
                RenderDirectionalShadows(cmd, i, split, tileSize);
            }

            cmd.SetGlobalInteger(CascadeCountId, _vsmSettings.Directional.CascadeCount);
            cmd.SetGlobalVectorArray(CascadeCullingSpheresId, _cascadeCullingSpheres);

            if (_vsmSettings.Blur != ToonVsmShadowSettings.BlurMode.None)
            {
                BakeViewSpaceZIntoMatrix();
            }

            cmd.SetGlobalMatrixArray(DirectionalShadowsMatricesVpId, _directionalShadowMatricesVp);
            cmd.SetGlobalMatrixArray(DirectionalShadowsMatricesVId, _directionalShadowMatricesV);

            ExecuteBuffer(cmd);
        }

        private void BakeViewSpaceZIntoMatrix()
        {
            for (int index = 0; index < _directionalShadowMatricesVp.Length; index++)
            {
                ref Matrix4x4 matrix = ref _directionalShadowMatricesVp[index];
                ref readonly Matrix4x4 viewMatrix = ref _directionalShadowMatricesV[index];

                float scale = DepthScale;
                if (SystemInfo.usesReversedZBuffer)
                {
                    scale *= -1;
                }

                matrix.m20 = scale * viewMatrix.m20;
                matrix.m21 = scale * viewMatrix.m21;
                matrix.m22 = scale * viewMatrix.m22;
                matrix.m23 = scale * viewMatrix.m23;
            }
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

        private void RenderDirectionalShadows(CommandBuffer cmd, int index, int split, int tileSize)
        {
            ShadowedDirectionalLight light = _shadowedDirectionalLights[index];
            var shadowSettings = new ShadowDrawingSettings(_cullingResults, light.VisibleLightIndex);
            int cascadeCount = _vsmSettings.Directional.CascadeCount;
            int tileOffset = index * cascadeCount;
            Vector3 ratios = _vsmSettings.Directional.GetRatios();

            cmd.BeginSample(RenderShadowsSample);
            cmd.SetGlobalDepthBias(0.0f, _vsmSettings.Directional.SlopeBias);
            cmd.SetGlobalVector(ShadowBiasId,
                new Vector4(-_vsmSettings.Directional.DepthBias, _vsmSettings.Directional.NormalBias)
            );

            for (int i = 0; i < cascadeCount; i++)
            {
                using (new ProfilingScope(cmd, NamedProfilingSampler.Get(CascadeProfilingNames[i])))
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
                    SetTileViewport(cmd, tileIndex, split, tileSize, out Vector2 offset);
                    _directionalShadowMatricesVp[tileIndex] =
                        ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, split);
                    _directionalShadowMatricesV[tileIndex] = viewMatrix;
                    cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

                    ExecuteBuffer(cmd);

                    _context.DrawShadows(ref shadowSettings);
                }
            }

            cmd.SetGlobalDepthBias(0f, 0.0f);
            cmd.EndSample(RenderShadowsSample);
            ExecuteBuffer(cmd);

            ExecuteBlur(cmd);
        }

        private void ExecuteBlur(CommandBuffer cmd)
        {
            // TODO: try using _blurCmd.SetKeyword
            if (_vsmSettings.Blur != ToonVsmShadowSettings.BlurMode.None)
            {
                cmd.BeginSample(BlurSample);
                bool highQualityBlur = _vsmSettings.Blur == ToonVsmShadowSettings.BlurMode.HighQuality;
                _blurMaterial.SetKeyword(new LocalKeyword(_blurShader, BlurHighQualityKeywordName),
                    highQualityBlur
                );

                _blurMaterial.SetKeyword(new LocalKeyword(_blurShader, BlurEarlyBailKeywordName),
                    _vsmSettings.IsBlurEarlyBailEnabled
                );
                if (_vsmSettings.IsBlurEarlyBailEnabled)
                {
                    _blurMaterial.SetFloat(EarlyBailThresholdId, _vsmSettings.BlurEarlyBailThreshold * DepthScale);
                }

                // Horizontal
                {
                    cmd.SetRenderTarget(DirectionalShadowsAtlasTempId,
                        RenderBufferLoadAction.DontCare,
                        RenderBufferStoreAction.Store,
                        DirectionalShadowsAtlasDepthId,
                        RenderBufferLoadAction.Load,
                        RenderBufferStoreAction.Store
                    );
                    Color shadowmapClearColor = GetShadowmapClearColor();
                    cmd.ClearRenderTarget(false, true, shadowmapClearColor);
                    CustomBlitter.Blit(cmd, _blurMaterial);
                }

                // Vertical
                {
                    cmd.SetRenderTarget(DirectionalShadowsAtlasId,
                        RenderBufferLoadAction.Load,
                        RenderBufferStoreAction.Store,
                        DirectionalShadowsAtlasDepthId,
                        RenderBufferLoadAction.Load,
                        RenderBufferStoreAction.Store
                    );
                    CustomBlitter.Blit(cmd, _blurMaterial, 1);
                }


                cmd.EndSample(BlurSample);
            }

            ExecuteBuffer(cmd);
        }

        private static void SetTileViewport(CommandBuffer cmd, int index, int split, int tileSize, out Vector2 offset)
        {
            // ReSharper disable once PossibleLossOfFraction
            offset = new Vector2(index % split, index / split);
            cmd.SetViewport(new Rect(
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