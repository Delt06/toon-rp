using System;
using DELTation.ToonRP.Lighting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using static DELTation.ToonRP.Shadows.ToonShadowMapsSettings;

namespace DELTation.ToonRP.Shadows
{
    public sealed class ToonShadowMaps : IDisposable
    {
        private const string RenderShadowsSample = "Render Shadows";
        private const string BlurSample = "Blur";

        private const int MaxShadowedDirectionalLightsCount = 1;
        private const int MaxShadowedAdditionalLightsCount = 16;
        public const int MaxCascades = 4;

        // Should mirror the value in VSM.hlsl
        private const float DepthScale = 0.1f;
        private const FilterMode ShadowmapFiltering = FilterMode.Bilinear;
        private const RenderTextureFormat DepthRenderTextureFormat = RenderTextureFormat.Shadowmap;

        public const string BlurShaderName = "Hidden/Toon RP/VSM Blur";
        public const string BlurHighQualityKeywordName = "_TOON_RP_VSM_BLUR_HIGH_QUALITY";
        public const string BlurEarlyBailKeywordName = "_TOON_RP_VSM_BLUR_EARLY_BAIL";
        private const int MaxPoissonDiskSize = 16;
        private static readonly int DirectionalShadowPoissonDiskId =
            Shader.PropertyToID("_ToonRP_DirectionalShadowPoissonDisk");

        private static readonly string[] CascadeProfilingNames;

        private static readonly int DirectionalShadowsAtlasId = Shader.PropertyToID("_ToonRP_DirectionalShadowAtlas");
        private static readonly int DirectionalShadowsAtlasDepthId =
            Shader.PropertyToID("_ToonRP_DirectionalShadowAtlas_Depth");
        private static readonly int DirectionalShadowsAtlasTempId =
            Shader.PropertyToID("_ToonRP_DirectionalShadowAtlas_Temp");
        private static readonly int DirectionalShadowsMatricesVpId =
            Shader.PropertyToID("_ToonRP_DirectionalShadowMatrices_VP");
        private static readonly int CascadeCountId =
            Shader.PropertyToID("_ToonRP_CascadeCount");
        private static readonly int CascadeCullingSpheresId =
            Shader.PropertyToID("_ToonRP_CascadeCullingSpheres");
        private static readonly int ShadowBiasId =
            Shader.PropertyToID("_ToonRP_ShadowBias");
        private static readonly int EarlyBailThresholdId = Shader.PropertyToID("_EarlyBailThreshold");
        private static readonly int LightBleedingReductionId =
            Shader.PropertyToID("_ToonRP_ShadowLightBleedingReduction");
        private static readonly int PrecisionCompensationId =
            Shader.PropertyToID("_ToonRP_ShadowPrecisionCompensation");
        private static readonly int BlurScatterId = Shader.PropertyToID("_ToonRP_VSM_BlurScatter");
        private static readonly int PoissonDiskSizeId = Shader.PropertyToID("_ToonRP_PoissonDiskSize");
        private static readonly int FPoissonDiskSizeId = Shader.PropertyToID("_ToonRP_fPoissonDiskSize");
        private static readonly int InvPoissonDiskSizeId = Shader.PropertyToID("_ToonRP_InvPoissonDiskSize");
        private static readonly int RotatedPoissonSamplingTextureId =
            Shader.PropertyToID("_ToonRP_RotatedPoissonSamplingTexture");

        private static readonly int LightDirectionPositionId = Shader.PropertyToID("_ToonRP_LightDirectionPosition");
        private static readonly int AdditionalShadowsDepthId = Shader.PropertyToID("_ToonRP_AdditionalShadows");
        private static readonly int AdditionalShadowsVpId = Shader.PropertyToID("_ToonRP_AdditionalShadowMatrices_VP");
        private static readonly int AdditionalShadowsMetadataId = Shader.PropertyToID("_ToonRP_AdditionalShadows_Metadata");
        
        private static readonly int ZClipId = Shader.PropertyToID("_ZClip");

        private static readonly Vector2[] PoissonDiskRaw =
        {
            new(-0.94201624f, -0.39906216f),
            new(0.94558609f, -0.76890725f),
            new(-0.094184101f, -0.92938870f),
            new(0.34495938f, 0.29387760f),
            new(-0.91588581f, 0.45771432f),
            new(-0.81544232f, -0.87912464f),
            new(-0.38277543f, 0.27676845f),
            new(0.97484398f, 0.75648379f),
            new(0.44323325f, -0.97511554f),
            new(0.53742981f, -0.47373420f),
            new(-0.26496911f, -0.41893023f),
            new(0.79197514f, 0.19090188f),
            new(-0.24188840f, 0.99706507f),
            new(-0.81409955f, 0.91437590f),
            new(0.19984126f, 0.78641367f),
            new(0.14383161f, -0.14100790f),
        };

        private readonly Matrix4x4[] _additionalShadowMatricesVp = new Matrix4x4[MaxShadowedAdditionalLightsCount];
        private readonly Vector4[] _additionalShadowMetadata = new Vector4[MaxShadowedAdditionalLightsCount];
        private readonly ToonPipelineMaterial _blurMaterial;
        private readonly Shader _blurShader;
        private readonly Vector4[] _cascadeCullingSpheres = new Vector4[MaxCascades];
        private readonly Matrix4x4[] _directionalShadowMatricesV = new Matrix4x4[MaxShadowedDirectionalLightsCount * MaxCascades];
        private readonly Matrix4x4[] _directionalShadowMatricesVp = new Matrix4x4[MaxShadowedDirectionalLightsCount * MaxCascades];
        private readonly Vector4[] _poissonDiskAdjusted = new Vector4[MaxPoissonDiskSize];

        private readonly ShadowedDirectionalLight[] _shadowedDirectionalLights =
            new ShadowedDirectionalLight[MaxShadowedDirectionalLightsCount];
        private ToonCameraRendererSettings _cameraRendererSettings;
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;
        private ToonShadowSettings _settings;
        private int _shadowedDirectionalLightCount;
        private ToonShadowMapsSettings _shadowMapsSettings;

        static ToonShadowMaps()
        {
            CascadeProfilingNames = new string[MaxCascades];

            for (int i = 0; i < MaxCascades; i++)
            {
                CascadeProfilingNames[i] = $"Cascade {i}";
            }
        }

        public ToonShadowMaps()
        {
            _blurShader = Shader.Find(BlurShaderName);
            _blurMaterial = new ToonPipelineMaterial(_blurShader, "Toon RP VSM Blur");
        }

        private string DirectionalShadowPassName
        {
            get
            {
                string passName = _shadowMapsSettings.Blur == BlurMode.None
                    ? ToonRpPassId.MainLightShadows
                    : ToonRpPassId.MainLightVsmShadows;
                return passName;
            }
        }

        public void Dispose()
        {
            _blurMaterial.Dispose();
        }

        public void Setup(in ScriptableRenderContext context,
            in CullingResults cullingResults,
            in ToonShadowSettings settings,
            in ToonCameraRendererSettings cameraRendererSettings
        )
        {
            _context = context;
            _cullingResults = cullingResults;
            _settings = settings;
            _cameraRendererSettings = cameraRendererSettings;
            _shadowMapsSettings = settings.ShadowMaps;
            _shadowedDirectionalLightCount = 0;
        }

        public void ReserveDirectionalShadows(Light light, int visibleLightIndex)
        {
            if (_shadowedDirectionalLightCount < MaxShadowedDirectionalLightsCount &&
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

        public void Render(in ToonLightsData lightsData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            (GraphicsFormat _, int depthBits) =
                ToonShadowMapFormatUtils.GetSupportedShadowMapFormat(_shadowMapsSettings.GetShadowMapDepthBits());

            // TODO: check if additional lights are enabled
            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.AdditionalLightsShadows)))
            {
                RenderAdditionalShadows(cmd, depthBits, lightsData);
            }

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(DirectionalShadowPassName)))
            {
                if (_shadowMapsSettings.Directional.Enabled && _shadowedDirectionalLightCount > 0)
                {
                    bool useCascades = _shadowMapsSettings.Directional.CascadeCount > 1;
                    cmd.SetKeyword(ToonShadows.DirectionalShadowsGlobalKeyword, !useCascades);
                    cmd.SetKeyword(ToonShadows.DirectionalCascadedShadowsGlobalKeyword, useCascades);
                    cmd.SetKeyword(ToonShadows.VsmGlobalKeyword,
                        _shadowMapsSettings.Blur != BlurMode.None
                    );
                    cmd.SetKeyword(ToonShadows.PcfGlobalKeyword,
                        _shadowMapsSettings is { Blur: BlurMode.None, SoftShadows: { Enabled: true } }
                    );
                    cmd.SetKeyword(ToonShadows.PoissonStratifiedGlobalKeyword,
                        _shadowMapsSettings is
                        {
                            Blur: BlurMode.None,
                            SoftShadows: { Enabled: true, Mode: SoftShadowsMode.PoissonStratified },
                        }
                    );
                    cmd.SetKeyword(ToonShadows.PoissonRotatedGlobalKeyword,
                        _shadowMapsSettings is
                        {
                            Blur: BlurMode.None, SoftShadows: { Enabled: true, Mode: SoftShadowsMode.PoissonRotated },
                        }
                    );
                    cmd.SetKeyword(ToonShadows.PoissonEarlyBailGlobalKeyword,
                        _shadowMapsSettings is
                        {
                            Blur: BlurMode.None,
                            SoftShadows: { Enabled: true, Quality: SoftShadowsQuality.High, EarlyBail: true },
                        }
                    );
                    cmd.SetKeyword(ToonShadows.ShadowsRampCrisp, _settings.CrispAntiAliased);
                    cmd.SetGlobalFloat(ZClipId, 0);

                    RenderDirectionalShadows(cmd, depthBits);
                }
                else
                {
                    cmd.GetTemporaryRT(DirectionalShadowsAtlasId, 1, 1,
                        depthBits,
                        ShadowmapFiltering,
                        DepthRenderTextureFormat
                    );
                    cmd.DisableKeyword(ToonShadows.DirectionalShadowsGlobalKeyword);
                    cmd.DisableKeyword(ToonShadows.DirectionalCascadedShadowsGlobalKeyword);
                    cmd.DisableKeyword(ToonShadows.VsmGlobalKeyword);
                    cmd.DisableKeyword(ToonShadows.PcfGlobalKeyword);
                    cmd.DisableKeyword(ToonShadows.PoissonStratifiedGlobalKeyword);
                    cmd.DisableKeyword(ToonShadows.PoissonRotatedGlobalKeyword);
                    cmd.DisableKeyword(ToonShadows.PoissonEarlyBailGlobalKeyword);
                    cmd.DisableKeyword(ToonShadows.ShadowsRampCrisp);
                }
            }

            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Cleanup()
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.ReleaseTemporaryRT(DirectionalShadowsAtlasId);
            cmd.ReleaseTemporaryRT(DirectionalShadowsAtlasDepthId);
            cmd.ReleaseTemporaryRT(DirectionalShadowsAtlasTempId);
            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void RenderDirectionalShadows(CommandBuffer cmd, int depthBits)
        {
            int atlasSize = (int) _shadowMapsSettings.Directional.AtlasSize;

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Prepare Shadowmaps")))
            {
                if (_shadowMapsSettings.Blur != BlurMode.None)
                {
                    GraphicsFormat shadowmapColorFormat =
                        ToonShadowMapFormatUtils.GetSupportedVsmTextureFormat(_shadowMapsSettings.VsmPrecision);
                    cmd.GetTemporaryRT(DirectionalShadowsAtlasId, atlasSize, atlasSize,
                        0,
                        ShadowmapFiltering,
                        shadowmapColorFormat
                    );
                    cmd.GetTemporaryRT(DirectionalShadowsAtlasDepthId, atlasSize, atlasSize,
                        depthBits,
                        ShadowmapFiltering,
                        DepthRenderTextureFormat
                    );
                    cmd.GetTemporaryRT(DirectionalShadowsAtlasTempId, atlasSize, atlasSize,
                        0,
                        ShadowmapFiltering,
                        shadowmapColorFormat
                    );
                    int firstRenderTarget = _shadowMapsSettings.Blur == BlurMode.Box
                        ? DirectionalShadowsAtlasTempId
                        : DirectionalShadowsAtlasId;
                    cmd.SetRenderTarget(firstRenderTarget,
                        RenderBufferLoadAction.DontCare,
                        RenderBufferStoreAction.Store,
                        DirectionalShadowsAtlasDepthId,
                        RenderBufferLoadAction.DontCare,
                        RenderBufferStoreAction.Store
                    );
                }
                else
                {
                    cmd.GetTemporaryRT(DirectionalShadowsAtlasId, atlasSize, atlasSize,
                        depthBits,
                        ShadowmapFiltering,
                        DepthRenderTextureFormat
                    );
                    cmd.SetRenderTarget(DirectionalShadowsAtlasId,
                        RenderBufferLoadAction.DontCare,
                        RenderBufferStoreAction.Store
                    );
                }


                cmd.ClearRenderTarget(true, true, GetShadowmapClearColor());
            }

            _context.ExecuteCommandBufferAndClear(cmd);

            int tiles = _shadowedDirectionalLightCount * _shadowMapsSettings.Directional.CascadeCount;
            int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            int tileSize = atlasSize / split;

            for (int i = 0; i < _shadowedDirectionalLightCount; ++i)
            {
                RenderDirectionalShadows(cmd, i, split, tileSize);
            }

            cmd.SetGlobalInteger(CascadeCountId, _shadowMapsSettings.Directional.CascadeCount);
            cmd.SetGlobalVectorArray(CascadeCullingSpheresId, _cascadeCullingSpheres);

            if (_shadowMapsSettings.Blur != BlurMode.None)
            {
                BakeViewSpaceZIntoMatrix();
            }
            else if (_shadowMapsSettings.SoftShadows.Enabled)
            {
                int poissonDiskSize = _shadowMapsSettings.SoftShadows.Quality switch
                {
                    SoftShadowsQuality.Low => 4,
                    SoftShadowsQuality.High => 16,
                    _ => throw new ArgumentOutOfRangeException(),
                };
                cmd.SetGlobalInt(PoissonDiskSizeId, poissonDiskSize);
                cmd.SetGlobalFloat(FPoissonDiskSizeId, poissonDiskSize);
                cmd.SetGlobalFloat(InvPoissonDiskSizeId, 1.0f / poissonDiskSize);

                const int spreadReferenceResolution = 1024;
                float spread = _shadowMapsSettings.SoftShadows.Spread / spreadReferenceResolution;
                cmd.SetGlobalTexture(RotatedPoissonSamplingTextureId,
                    _shadowMapsSettings.SoftShadows.RotatedPoissonSamplingTexture
                );
                FillPoissonDiskValues(poissonDiskSize, spread);
                cmd.SetGlobalVectorArray(DirectionalShadowPoissonDiskId, _poissonDiskAdjusted);
            }

            cmd.SetGlobalMatrixArray(DirectionalShadowsMatricesVpId, _directionalShadowMatricesVp);
            if (_shadowMapsSettings.Blur != BlurMode.None)
            {
                cmd.SetGlobalFloat(LightBleedingReductionId, _shadowMapsSettings.LightBleedingReduction);
                cmd.SetGlobalFloat(PrecisionCompensationId, _shadowMapsSettings.PrecisionCompensation);
            }

            _context.ExecuteCommandBufferAndClear(cmd);
        }

        private static void BindLightParams(CommandBuffer cmd, Light light, float depthBias, float normalBias, float slopeBias)
        {
            cmd.SetGlobalDepthBias(0.0f, slopeBias);
            cmd.SetGlobalVector(ShadowBiasId, PackShadowBias(depthBias, normalBias));
            cmd.SetGlobalVector(LightDirectionPositionId, PackLightDirectionPosition(light));
        }

        private static Vector4 PackShadowBias(float depthBias, float normalBias) => new(-depthBias, normalBias);

        private static Vector4 PackLightDirectionPosition(Light light)
        {
            Vector4 result;

            if (light.type == LightType.Directional)
            {
                result = -light.transform.forward;
                result.w = 0.0f;
            }
            else
            {
                result = light.transform.position;
                result.w = 1.0f;
            }

            return result;
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

        private void FillPoissonDiskValues(int diskSize, float spread)
        {
            for (int i = 0; i < diskSize; ++i)
            {
                _poissonDiskAdjusted[i] = PoissonDiskRaw[i] * spread;
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
            ShadowedDirectionalLight shadowedLight = _shadowedDirectionalLights[index];
            var shadowSettings =
                new ShadowDrawingSettings(_cullingResults, shadowedLight.VisibleLightIndex
#if UNITY_2022_2_OR_NEWER
                    , BatchCullingProjectionType.Orthographic // directional shadows are rendered with orthographic projection
#endif // UNITY_2022_2_OR_NEWER
                );

            ref DirectionalShadows directionalShadowSettings = ref _shadowMapsSettings.Directional;
            int cascadeCount = directionalShadowSettings.CascadeCount;
            int tileOffset = index * cascadeCount;
            Vector3 ratios = directionalShadowSettings.GetRatios();

            cmd.BeginSample(RenderShadowsSample);
            Light light = _cullingResults.visibleLights[shadowedLight.VisibleLightIndex].light;
            BindLightParams(cmd, light, directionalShadowSettings.DepthBias, directionalShadowSettings.NormalBias, directionalShadowSettings.SlopeBias);

            for (int i = 0; i < cascadeCount; i++)
            {
                using (new ProfilingScope(cmd, NamedProfilingSampler.Get(CascadeProfilingNames[i])))
                {
                    _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                        shadowedLight.VisibleLightIndex, i, cascadeCount, ratios, tileSize, shadowedLight.NearPlaneOffset,
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
                        ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, split, _settings.ShadowMaps.Blur == BlurMode.None);
                    _directionalShadowMatricesV[tileIndex] = viewMatrix;
                    cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

                    _context.ExecuteCommandBufferAndClear(cmd);

                    _context.DrawShadows(ref shadowSettings);
                }
            }

            cmd.SetGlobalDepthBias(0f, 0.0f);
            cmd.EndSample(RenderShadowsSample);
            _context.ExecuteCommandBufferAndClear(cmd);

            ExecuteBlur(cmd);
        }

        private void RenderAdditionalShadows(CommandBuffer cmd, int depthBits, in ToonLightsData lightsData)
        {
            int shadowLightsCount = 0;

            if (_settings.ShadowMaps.Additional.Enabled && _cameraRendererSettings.AdditionalLights == ToonCameraRendererSettings.AdditionalLightsMode.PerPixel)
            {
                // Mark shadowed lights
                for (int additionalLightIndex = 0; additionalLightIndex < lightsData.AdditionalLights.Count; additionalLightIndex++)
                {
                    ToonLightsData.AdditionalLight additionalLight = lightsData.AdditionalLights[additionalLightIndex];
                    VisibleLight visibleLight = _cullingResults.visibleLights[additionalLight.VisibleLightIndex];
                    if (visibleLight.light.shadows == LightShadows.None)
                    {
                        continue;
                    }

                    if (additionalLightIndex >= _additionalShadowMetadata.Length)
                    {
                        continue;
                    }
                    
                    if (!_cullingResults.GetShadowCasterBounds(additionalLight.VisibleLightIndex, out Bounds _))
                    {
                        continue;
                    }

                    // TODO: add point light later
                    if (visibleLight.lightType is LightType.Spot)
                    {
                        additionalLight.ShadowLightIndex = shadowLightsCount;
                        lightsData.AdditionalLights[additionalLightIndex] = additionalLight;

                        ++shadowLightsCount;
                    }
                }

                var defaultMetadata = new Vector4(-1, 0, 0, 0);

                for (int i = 0; i < _additionalShadowMetadata.Length; i++)
                {
                    _additionalShadowMetadata[i] = defaultMetadata;
                }
            }

            if (shadowLightsCount > 0)
            {
                int resolution = (int) _settings.ShadowMaps.Additional.AtlasSize;
                int split = Mathf.CeilToInt(Mathf.Sqrt(shadowLightsCount));
                int tileSize = resolution / split;
                resolution = tileSize * split;
                
                cmd.SetGlobalFloat(ZClipId, 1);
                cmd.GetTemporaryRT(AdditionalShadowsDepthId, resolution, resolution, depthBits, ShadowmapFiltering, DepthRenderTextureFormat);
                cmd.SetRenderTarget(AdditionalShadowsDepthId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmd.ClearRenderTarget(true, false, Color.clear);

                for (int additionalLightIndex = 0; additionalLightIndex < lightsData.AdditionalLights.Count; additionalLightIndex++)
                {
                    ToonLightsData.AdditionalLight additionalLight = lightsData.AdditionalLights[additionalLightIndex];
                    if (additionalLight.ShadowLightIndex == null)
                    {
                        continue;
                    }

                    int visibleLightIndex = additionalLight.VisibleLightIndex;
                    Light light = _cullingResults.visibleLights[visibleLightIndex].light;
                    if (_cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(visibleLightIndex, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData _))
                    {
                        int shadowLightIndex = additionalLight.ShadowLightIndex.Value;
                        SetTileViewport(cmd, shadowLightIndex, split, tileSize, out Vector2 offset);
                        _additionalShadowMatricesVp[shadowLightIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, split);
                        _additionalShadowMetadata[additionalLightIndex] = new Vector4(shadowLightIndex, 0, 0, 0);

                        cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                        BindLightParams(cmd, light, light.shadowBias, -light.shadowNormalBias, 0.0f);

                        _context.ExecuteCommandBufferAndClear(cmd);

                        var shadowSettings = new ShadowDrawingSettings(_cullingResults, visibleLightIndex, BatchCullingProjectionType.Perspective);
                        _context.DrawShadows(ref shadowSettings);
                    }
                }

                cmd.SetGlobalMatrixArray(AdditionalShadowsVpId, _additionalShadowMatricesVp);
                cmd.SetGlobalVectorArray(AdditionalShadowsMetadataId, _additionalShadowMetadata);
                cmd.EnableKeyword(ToonShadows.AdditionalShadowsGlobalKeyword);
            }
            else
            {
                cmd.DisableKeyword(ToonShadows.AdditionalShadowsGlobalKeyword);
            }
        }

        private void ExecuteBlur(CommandBuffer cmd)
        {
            // TODO: try using _blurCmd.SetKeyword
            if (_shadowMapsSettings.Blur != BlurMode.None)
            {
                cmd.BeginSample(BlurSample);

                float blurScatter = Mathf.Max(1.0f, _settings.ShadowMaps.BlurScatter);
                cmd.SetGlobalFloat(BlurScatterId, blurScatter);

                const int gaussianHorizontalPass = 0;
                const int gaussianVerticalPass = 1;
                const int boxBlurPass = 2;

                Material blurMaterial = _blurMaterial.GetOrCreate();

                if (_shadowMapsSettings.Blur == BlurMode.Box)
                {
                    {
                        cmd.SetRenderTarget(DirectionalShadowsAtlasId,
                            RenderBufferLoadAction.Load,
                            RenderBufferStoreAction.Store,
                            DirectionalShadowsAtlasDepthId,
                            RenderBufferLoadAction.Load,
                            RenderBufferStoreAction.DontCare
                        );
                        Color shadowmapClearColor = GetShadowmapClearColor();
                        cmd.ClearRenderTarget(false, true, shadowmapClearColor);
                        ToonBlitter.Blit(cmd, blurMaterial, true, boxBlurPass);
                    }
                }
                else
                {
                    bool highQualityBlur = _shadowMapsSettings.Blur == BlurMode.GaussianHighQuality;
                    blurMaterial.SetKeyword(new LocalKeyword(_blurShader, BlurHighQualityKeywordName),
                        highQualityBlur
                    );

                    blurMaterial.SetKeyword(new LocalKeyword(_blurShader, BlurEarlyBailKeywordName),
                        _shadowMapsSettings.IsBlurEarlyBailEnabled
                    );
                    if (_shadowMapsSettings.IsBlurEarlyBailEnabled)
                    {
                        blurMaterial.SetFloat(EarlyBailThresholdId,
                            _shadowMapsSettings.BlurEarlyBailThreshold * DepthScale
                        );
                    }

                    // Horizontal
                    {
                        cmd.SetRenderTarget(DirectionalShadowsAtlasTempId,
                            RenderBufferLoadAction.Load,
                            RenderBufferStoreAction.Store,
                            DirectionalShadowsAtlasDepthId,
                            RenderBufferLoadAction.Load,
                            RenderBufferStoreAction.Store
                        );
                        Color shadowmapClearColor = GetShadowmapClearColor();
                        cmd.ClearRenderTarget(false, true, shadowmapClearColor);

                        // ReSharper disable once RedundantArgumentDefaultValue
                        ToonBlitter.Blit(cmd, blurMaterial, true, gaussianHorizontalPass);
                    }

                    // Vertical
                    {
                        cmd.SetRenderTarget(DirectionalShadowsAtlasId,
                            RenderBufferLoadAction.Load,
                            RenderBufferStoreAction.Store,
                            DirectionalShadowsAtlasDepthId,
                            RenderBufferLoadAction.Load,
                            RenderBufferStoreAction.DontCare
                        );
                        ToonBlitter.Blit(cmd, blurMaterial, true, gaussianVerticalPass);
                    }
                }


                cmd.EndSample(BlurSample);
            }

            _context.ExecuteCommandBufferAndClear(cmd);
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

        private static Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split, bool correctZ = true)
        {
            Matrix4x4 remap = Matrix4x4.identity;
            remap.m00 = remap.m11 = 0.5f; // scale [-1; 1] -> [-0.5, 0.5]
            remap.m03 = remap.m13 = 0.5f; // translate [-0.5, 0.5] -> [0, 1]

            if (correctZ)
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