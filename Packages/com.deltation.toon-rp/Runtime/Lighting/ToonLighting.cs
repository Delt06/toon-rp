using System;
using DELTation.ToonRP.Shadows;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using static DELTation.ToonRP.ToonCameraRendererSettings;

namespace DELTation.ToonRP.Lighting
{
    public enum ToonMixedLightingSetup
    {
        None,
        Subtractive,
        ShadowMask,
    }

    public sealed class ToonLighting
    {
        private const string CmdName = "Lighting";

        private const int MaxAdditionalLightCount = 64;

        // Mirrored with TiledLighting_Shared.hlsl
        public const int MaxAdditionalLightCountTiled = 1024;

        private static GlobalKeyword _additionalLightsGlobalKeyword;
        private static GlobalKeyword _additionalLightsVertexGlobalKeyword;
        private readonly Vector4[] _additionalLightColors = new Vector4[MaxAdditionalLightCount];
        private readonly Vector4[] _additionalLightPositions = new Vector4[MaxAdditionalLightCount];
        private readonly Vector4[] _additionalLightSpotDir = new Vector4[MaxAdditionalLightCount];

        private readonly CommandBuffer _cmd = new() { name = CmdName };
        private int _additionalLightsCount;
        private TiledLight[] _additionalTiledLights;
        private Vector4[] _additionalTiledLightsColors;
        private Vector4[] _additionalTiledLightsPositionWsAttenuations;
        private Camera _camera;
        private ToonCameraRendererSettings _cameraRendererSettings;
        private int _currentMaxAdditionalLights;
        private ToonMixedLightingSetup _mixedLightingSetup;

        public ToonLighting()
        {
            _additionalLightsGlobalKeyword = GlobalKeyword.Create(Keywords.AdditionalLightsGlobalKeyword);
            _additionalLightsVertexGlobalKeyword = GlobalKeyword.Create(Keywords.AdditionalLightsVertexGlobalKeyword);
        }

        public void Setup(ref ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults,
            in ToonCameraRendererSettings settings, in ToonShadowSettings shadowSettings,
            in VisibleLight? mainLight
        )
        {
            _camera = camera;

            _cameraRendererSettings = settings;
            _currentMaxAdditionalLights = _cameraRendererSettings.IsTiledLightingEnabledAndSupported()
                ? MaxAdditionalLightCountTiled
                : MaxAdditionalLightCount;
            _mixedLightingSetup = ToonMixedLightingSetup.None;

            if (_cameraRendererSettings.IsTiledLightingEnabledAndSupported())
            {
                _additionalTiledLights ??= new TiledLight[MaxAdditionalLightCountTiled];
                _additionalTiledLightsColors ??= new Vector4[MaxAdditionalLightCountTiled];
                _additionalTiledLightsPositionWsAttenuations ??= new Vector4[MaxAdditionalLightCountTiled];
            }

            _cmd.BeginSample(CmdName);
            SetupDirectionalLight(mainLight, shadowSettings);

            AdditionalLightsMode additionalLightsMode = _cameraRendererSettings.AdditionalLights;
            if (additionalLightsMode != AdditionalLightsMode.Off)
            {
                NativeArray<int> indexMap = cullingResults.GetLightIndexMap(Allocator.Temp);
                SetupAdditionalLights(indexMap, cullingResults.visibleLights, shadowSettings);
                cullingResults.SetLightIndexMap(indexMap);
                indexMap.Dispose();
            }

            SetMixedLightingKeywordsAndProperties();
            SetAdditionalLightsKeywords(additionalLightsMode);

            _cmd.EndSample(CmdName);
            context.ExecuteCommandBufferAndClear(_cmd);
        }

        private void SetupDirectionalLight(in VisibleLight? light, in ToonShadowSettings shadowSettings)
        {
            if (light != null)
            {
                VisibleLight visibleLight = light.Value;
                _cmd.SetGlobalVector(ShaderPropertyId.DirectionalLightColorId, visibleLight.finalColor);

                Vector4 direction = (visibleLight.localToWorldMatrix * Vector3.back).normalized;
                _cmd.SetGlobalVector(ShaderPropertyId.DirectionalLightDirection, direction);

                InitializeLightCommon(visibleLight, shadowSettings);
                LightBakingOutput lightBakingOutput = visibleLight.light.bakingOutput;
            }
            else
            {
                _cmd.SetGlobalVector(ShaderPropertyId.DirectionalLightColorId, Vector4.zero);
                _cmd.SetGlobalVector(ShaderPropertyId.DirectionalLightDirection, Vector4.zero);
            }
        }

        private void InitializeLightCommon(in VisibleLight visibleLight, in ToonShadowSettings shadowSettings)
        {
            LightBakingOutput lightBakingOutput = visibleLight.light.bakingOutput;

            if (_mixedLightingSetup == ToonMixedLightingSetup.None)
            {
                if (lightBakingOutput.lightmapBakeType == LightmapBakeType.Mixed &&
                    shadowSettings.Mode == ToonShadowSettings.ShadowMode.ShadowMapping &&
                    visibleLight.light.shadows != LightShadows.None)
                {
                    switch (lightBakingOutput.mixedLightingMode)
                    {
                        case MixedLightingMode.Subtractive:
                            _mixedLightingSetup = ToonMixedLightingSetup.Subtractive;
                            break;
                        case MixedLightingMode.Shadowmask:
                            _mixedLightingSetup = ToonMixedLightingSetup.ShadowMask;
                            break;
                    }
                }
            }
        }

        private void SetMixedLightingKeywordsAndProperties()
        {
            // TODO: check if enabled in the pipeline settings
            const bool supportsMixedLighting = true;
            bool isShadowMask = supportsMixedLighting && _mixedLightingSetup == ToonMixedLightingSetup.ShadowMask;
            bool isShadowMaskAlways = isShadowMask && QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask;
            bool isSubtractive = supportsMixedLighting && _mixedLightingSetup == ToonMixedLightingSetup.Subtractive;
            CoreUtils.SetKeyword(_cmd, Keywords.LightmapShadowMixingGlobalKeyword, isSubtractive || isShadowMaskAlways);
            CoreUtils.SetKeyword(_cmd, Keywords.ShadowsShadowMaskGlobalKeyword, isShadowMask);

            _cmd.SetGlobalVector(ShaderPropertyId.SubtractiveShadowColor,
                CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.subtractiveShadowColor)
            );
        }

        private void SetAdditionalLightsKeywords(AdditionalLightsMode lightsMode)
        {
            bool anyAdditionalLights = _additionalLightsCount > 0;
            (bool enablePerPixel, bool enablePerVertex) = (anyAdditionalLights, lightsMode) switch
            {
                (_, AdditionalLightsMode.Off) => (false, false),
                (false, _) => (false, false),
                (true, AdditionalLightsMode.PerPixel) => (true, false),
                (true, AdditionalLightsMode.PerVertex) => (false, true),
                _ => throw new ArgumentOutOfRangeException(),
            };
            _cmd.SetKeyword(_additionalLightsGlobalKeyword, enablePerPixel);
            _cmd.SetKeyword(_additionalLightsVertexGlobalKeyword, enablePerVertex);
        }

        private void SetupAdditionalLights(NativeArray<int> indexMap, in NativeArray<VisibleLight> visibleLights,
            in ToonShadowSettings shadowSettings)
        {
            const int lightSkipIndex = -1;
            _additionalLightsCount = 0;

            for (int visibleLightIndex = 0; visibleLightIndex < visibleLights.Length; visibleLightIndex++)
            {
                VisibleLight visibleLight = visibleLights[visibleLightIndex];

                int newIndex = lightSkipIndex;

                switch (visibleLight.lightType)
                {
                    case LightType.Point:
                    case LightType.Spot:
                    {
                        // Currently, Tiled Lighting only supports Point lights
                        // https://github.com/Delt06/toon-rp/issues/229
                        if (visibleLight.lightType != LightType.Point &&
                            _cameraRendererSettings.IsTiledLightingEnabledAndSupported())
                        {
                            break;
                        }

                        if (_additionalLightsCount < _currentMaxAdditionalLights)
                        {
                            newIndex = _additionalLightsCount;
                            SetupAdditionalLight(_additionalLightsCount, visibleLight, shadowSettings);
                            _additionalLightsCount++;
                        }

                        break;
                    }

                    case LightType.Directional:
                    case LightType.Area:
                    case LightType.Disc:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                indexMap[visibleLightIndex] = newIndex;
            }

            // Remove invisible lights
            for (int i = visibleLights.Length; i < indexMap.Length; ++i)
            {
                indexMap[i] = lightSkipIndex;
            }

            _cmd.SetGlobalInt(ShaderPropertyId.AdditionalLightCount, _additionalLightsCount);

            if (_additionalLightsCount > 0)
            {
                _cmd.SetGlobalVectorArray(ShaderPropertyId.AdditionalLightColors, _additionalLightColors);
                _cmd.SetGlobalVectorArray(ShaderPropertyId.AdditionalLightPositions, _additionalLightPositions);
                _cmd.SetGlobalVectorArray(ShaderPropertyId.AdditionalLightSpotDir, _additionalLightSpotDir);
            }
        }

        public void GetTiledAdditionalLightsBuffer(out TiledLight[] lights, out Vector4[] colors,
            out Vector4[] positionsAttenuations, out int count)
        {
            Assert.IsNotNull(_additionalTiledLights, "Tiled lights are not initialized");

            lights = _additionalTiledLights;
            colors = _additionalTiledLightsColors;
            positionsAttenuations = _additionalTiledLightsPositionWsAttenuations;
            count = _additionalLightsCount;
        }

        private void SetupAdditionalLight(int index, in VisibleLight visibleLight, in ToonShadowSettings shadowSettings)
        {
            InitializeLightCommon(visibleLight, shadowSettings);

            Vector4 color = visibleLight.finalColor;
            Matrix4x4 lightLocalToWorld = visibleLight.localToWorldMatrix;
            Vector3 positionWs = lightLocalToWorld.GetColumn(3);

            float distanceAttenuation = 1.0f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);

            var spotAttenuation = new Vector2(0.0f, 1.0f);
            Vector3 spotDir = default;

            if (visibleLight.lightType == LightType.Spot)
            {
                float? innerSpotAngle = visibleLight.light ? visibleLight.light.innerSpotAngle : null;
                GetSpotAngleAttenuation(visibleLight.spotAngle, innerSpotAngle, out spotAttenuation);
                GetSpotDirection(ref lightLocalToWorld, out spotDir);
            }

            if (index < MaxAdditionalLightCount)
            {
                _additionalLightColors[index] = new Vector4(color.x, color.y, color.z, distanceAttenuation);
                _additionalLightPositions[index] =
                    new Vector4(positionWs.x, positionWs.y, positionWs.z, spotAttenuation.x);
                _additionalLightSpotDir[index] = new Vector4(spotDir.x, spotDir.y, spotDir.z, spotAttenuation.y);
            }

            var positionWsAttenuation = new Vector4(positionWs.x, positionWs.y, positionWs.z, distanceAttenuation);

            if (_additionalTiledLights != null)
            {
                Vector4 positionVsRange =
                    _camera.worldToCameraMatrix.MultiplyPoint(lightLocalToWorld.GetColumn(3));
                positionVsRange.w = visibleLight.range;

                ref TiledLight tiledLight = ref _additionalTiledLights[index];
                tiledLight.Color = color;
                tiledLight.PositionVsRange = positionVsRange;
                tiledLight.PositionWsAttenuation = positionWsAttenuation;
            }

            if (_additionalTiledLightsColors != null)
            {
                _additionalTiledLightsColors[index] = color;
            }

            if (_additionalTiledLightsPositionWsAttenuations != null)
            {
                _additionalTiledLightsPositionWsAttenuations[index] = positionWsAttenuation;
            }
        }

        private static void GetSpotAngleAttenuation(
            float spotAngle, float? innerSpotAngle,
            out Vector2 spotAttenuation)
        {
            // Spot Attenuation with a linear falloff can be defined as
            // (SdotL - cosOuterAngle) / (cosInnerAngle - cosOuterAngle)
            // This can be rewritten as
            // invAngleRange = 1.0 / (cosInnerAngle - cosOuterAngle)
            // SdotL * invAngleRange + (-cosOuterAngle * invAngleRange)
            // If we precompute the terms in a MAD instruction
            float cosOuterAngle = Mathf.Cos(Mathf.Deg2Rad * spotAngle * 0.5f);
            // We need to do a null check for particle lights
            // This should be changed in the future
            // Particle lights will use an inline function
            float cosInnerAngle;
            if (innerSpotAngle.HasValue)
            {
                cosInnerAngle = Mathf.Cos(innerSpotAngle.Value * Mathf.Deg2Rad * 0.5f);
            }
            else
            {
                cosInnerAngle = Mathf.Cos(2.0f *
                                          Mathf.Atan(Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad) * (64.0f - 18.0f) /
                                                     64.0f
                                          ) * 0.5f
                );
            }

            float smoothAngleRange = Mathf.Max(0.001f, cosInnerAngle - cosOuterAngle);
            float invAngleRange = 1.0f / smoothAngleRange;
            float add = -cosOuterAngle * invAngleRange;

            spotAttenuation.x = invAngleRange;
            spotAttenuation.y = add;
        }

        private static void GetSpotDirection(ref Matrix4x4 lightLocalToWorldMatrix, out Vector3 spotDir)
        {
            Vector4 dir = lightLocalToWorldMatrix.GetColumn(2);
            spotDir = new Vector4(-dir.x, -dir.y, -dir.z);
        }

        private static class ShaderPropertyId
        {
            public static readonly int SubtractiveShadowColor = Shader.PropertyToID("_SubtractiveShadowColor");
            public static readonly int DirectionalLightColorId = Shader.PropertyToID("_DirectionalLightColor");

            public static readonly int DirectionalLightDirection = Shader.PropertyToID("_DirectionalLightDirection");
            public static readonly int AdditionalLightCount = Shader.PropertyToID("_AdditionalLightCount");
            public static readonly int AdditionalLightColors = Shader.PropertyToID("_AdditionalLightColors");
            public static readonly int AdditionalLightPositions = Shader.PropertyToID("_AdditionalLightPositions");
            public static readonly int AdditionalLightSpotDir = Shader.PropertyToID("_AdditionalLightSpotDir");
        }

        public static class Keywords
        {
            public const string LightmapShadowMixingGlobalKeyword = "LIGHTMAP_SHADOW_MIXING";
            public const string ShadowsShadowMaskGlobalKeyword = "SHADOWS_SHADOWMASK";
            public const string AdditionalLightsGlobalKeyword = "_TOON_RP_ADDITIONAL_LIGHTS";
            public const string AdditionalLightsVertexGlobalKeyword = "_TOON_RP_ADDITIONAL_LIGHTS_VERTEX";
        }
    }
}