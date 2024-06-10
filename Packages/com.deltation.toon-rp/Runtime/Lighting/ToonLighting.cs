using System;
using DELTation.ToonRP.Shadows;
using Unity.Collections;
using Unity.Mathematics;
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
        private Camera _camera;
        private ToonCameraRendererSettings _cameraRendererSettings;
        private int _currentMaxAdditionalLights;
        private ToonMixedLightingSetup _mixedLightingSetup;
        private TiledAdditionalLightsData _tiledData;

        public ToonLighting()
        {
            _additionalLightsGlobalKeyword = GlobalKeyword.Create(Keywords.AdditionalLights);
            _additionalLightsVertexGlobalKeyword = GlobalKeyword.Create(Keywords.AdditionalLightsVertex);
        }

        public void Setup(ref ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults,
            in ToonCameraRendererSettings settings, in ToonShadowSettings shadowSettings,
            in VisibleLight? mainLight, ref ToonLightsData lightsData
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
                _tiledData.EnsureBuffersAreCreated();
            }

            _cmd.BeginSample(CmdName);
            SetupDirectionalLight(mainLight, shadowSettings);

            AdditionalLightsMode additionalLightsMode = _cameraRendererSettings.AdditionalLights;
            if (additionalLightsMode != AdditionalLightsMode.Off)
            {
                NativeArray<int> indexMap = cullingResults.GetLightIndexMap(Allocator.Temp);
                SetupAdditionalLights(indexMap, cullingResults.visibleLights, shadowSettings, ref lightsData);
                cullingResults.SetLightIndexMap(indexMap);
                indexMap.Dispose();
            }

            SetMixedLightingKeywordsAndProperties(settings);
            SetAdditionalLightsKeywords(_cmd, additionalLightsMode);

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
                _cmd.SetGlobalVector(ShaderPropertyId.DirectionalLightDirectionId, direction);

                InitializeLightCommon(visibleLight, shadowSettings);
                LightBakingOutput lightBakingOutput = visibleLight.light.bakingOutput;

                Vector4 occlusionProbeChannel = Vector4.zero;
                if (lightBakingOutput is
                    { lightmapBakeType: LightmapBakeType.Mixed, occlusionMaskChannel: >= 0 and < 4 })
                {
                    occlusionProbeChannel[lightBakingOutput.occlusionMaskChannel] = 1.0f;
                }

                _cmd.SetGlobalVector(ShaderPropertyId.DirectionalLightOcclusionProbesId, occlusionProbeChannel);
            }
            else
            {
                _cmd.SetGlobalVector(ShaderPropertyId.DirectionalLightColorId, Vector4.zero);
                _cmd.SetGlobalVector(ShaderPropertyId.DirectionalLightDirectionId, Vector4.zero);
                _cmd.SetGlobalVector(ShaderPropertyId.DirectionalLightOcclusionProbesId, Vector4.zero);
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

        private void SetMixedLightingKeywordsAndProperties(in ToonCameraRendererSettings settings)
        {
            bool mixedLightingEnabled = (settings.BakedLightingFeatures & ToonRpBakedLightingFeatures.LightMaps) != 0;
            bool shadowMaskEnabled = (settings.BakedLightingFeatures & ToonRpBakedLightingFeatures.ShadowMask) != 0;
            bool isShadowMask = shadowMaskEnabled && _mixedLightingSetup == ToonMixedLightingSetup.ShadowMask;
            bool isShadowMaskAlways = isShadowMask && QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask;
            bool isSubtractive = mixedLightingEnabled && _mixedLightingSetup == ToonMixedLightingSetup.Subtractive;
            CoreUtils.SetKeyword(_cmd, Keywords.LightmapShadowMixing, isSubtractive || isShadowMaskAlways);
            CoreUtils.SetKeyword(_cmd, Keywords.ShadowsShadowMask, isShadowMask);

            _cmd.SetGlobalVector(ShaderPropertyId.SubtractiveShadowColor,
                CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.subtractiveShadowColor)
            );
        }
        
        public void SetAdditionalLightsKeywords(CommandBuffer cmd, AdditionalLightsMode lightsMode)
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
            cmd.SetKeyword(_additionalLightsGlobalKeyword, enablePerPixel);
            cmd.SetKeyword(_additionalLightsVertexGlobalKeyword, enablePerVertex);
        }

        private void SetupAdditionalLights(NativeArray<int> indexMap, in NativeArray<VisibleLight> visibleLights,
            in ToonShadowSettings shadowSettings, ref ToonLightsData lightsData)
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
                        if (_additionalLightsCount < _currentMaxAdditionalLights)
                        {
                            newIndex = _additionalLightsCount;
                            SetupAdditionalLight(_additionalLightsCount, visibleLight, shadowSettings);
                            lightsData.AdditionalLights.Add(new ToonLightsData.AdditionalLight
                                {
                                    VisibleLightIndex = visibleLightIndex,
                                    ShadowLightIndex = null,
                                }
                            );
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

        public ref readonly TiledAdditionalLightsData GetTiledAdditionalLightsBuffers()
        {
            Assert.IsNotNull(_tiledData.TiledLights, "Tiled lights are not initialized");
            return ref _tiledData;
        }

        public int GetAdditionalLightsCount() => _additionalLightsCount;

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
            half2 spotAttenuationHalf = math.half2((half) spotAttenuation.x, (half) spotAttenuation.y);
            float spotAttenuationPacked = math.asfloat(spotAttenuationHalf.x.value << 16 | spotAttenuationHalf.y.value);
            var spotDirAttenuationPacked = new Vector4(spotDir.x, spotDir.y, spotDir.z, spotAttenuationPacked);

            if (_tiledData.TiledLights != null)
            {
                Vector4 boundingSphereCenterVsRadius = _camera.worldToCameraMatrix.MultiplyPoint(lightLocalToWorld.GetColumn(3));
                boundingSphereCenterVsRadius.w = visibleLight.range;

                ref TiledLight tiledLight = ref _tiledData.TiledLights[index];
                tiledLight.Color = color;
                tiledLight.BoundingSphere_CenterVs_Radius = boundingSphereCenterVsRadius;
                tiledLight.PositionWs_Attenuation = positionWsAttenuation;
                
                if (visibleLight.lightType == LightType.Spot)
                {
                    // https://wickedengine.net/2018/01/optimizing-tile-based-light-culling/
                    float spotAngleCos = Mathf.Cos(Mathf.Deg2Rad * visibleLight.spotAngle * 0.5f);
                    float boundingSphereRadius = visibleLight.range * 0.5f / spotAngleCos;
                    Vector4 boundingSphereCenter = lightLocalToWorld.GetColumn(3) - (Vector4)(spotDir * boundingSphereRadius);

                    Vector4 coneBoundingSphereCenterVsRadius = _camera.worldToCameraMatrix.MultiplyPoint(boundingSphereCenter);
                    coneBoundingSphereCenterVsRadius.w = boundingSphereRadius;
                    tiledLight.ConeBoundingSphere_CenterVs_Radius = coneBoundingSphereCenterVsRadius;
                }
                else
                {
                    tiledLight.ConeBoundingSphere_CenterVs_Radius = default;
                }
            }

            if (_tiledData.Colors != null)
            {
                _tiledData.Colors[index] = color;
            }

            if (_tiledData.PositionsAttenuations != null)
            {
                _tiledData.PositionsAttenuations[index] = positionWsAttenuation;
            }

            if (_tiledData.SpotDirsAttenuations != null)
            {
                _tiledData.SpotDirsAttenuations[index] = spotDirAttenuationPacked;
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

        public struct TiledAdditionalLightsData
        {
            public TiledLight[] TiledLights;
            public Vector4[] Colors;
            public Vector4[] PositionsAttenuations;
            public Vector4[] SpotDirsAttenuations;

            public void EnsureBuffersAreCreated()
            {
                TiledLights ??= new TiledLight[MaxAdditionalLightCountTiled];
                Colors ??= new Vector4[MaxAdditionalLightCountTiled];
                PositionsAttenuations ??= new Vector4[MaxAdditionalLightCountTiled];
                SpotDirsAttenuations ??= new Vector4[MaxAdditionalLightCountTiled];
            }
        }

        private static class ShaderPropertyId
        {
            public static readonly int SubtractiveShadowColor = Shader.PropertyToID("_SubtractiveShadowColor");

            public static readonly int DirectionalLightColorId = Shader.PropertyToID("_DirectionalLightColor");
            public static readonly int DirectionalLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");

            public static readonly int DirectionalLightOcclusionProbesId =
                Shader.PropertyToID("_DirectionalLightOcclusionProbes");

            public static readonly int AdditionalLightCount = Shader.PropertyToID("_AdditionalLightCount");
            public static readonly int AdditionalLightColors = Shader.PropertyToID("_AdditionalLightColors");
            public static readonly int AdditionalLightPositions = Shader.PropertyToID("_AdditionalLightPositions");
            public static readonly int AdditionalLightSpotDir = Shader.PropertyToID("_AdditionalLightSpotDir");
        }

        public static class Keywords
        {
            public const string LightmapShadowMixing = "LIGHTMAP_SHADOW_MIXING";
            public const string ShadowsShadowMask = "SHADOWS_SHADOWMASK";
            public const string AdditionalLights = "_TOON_RP_ADDITIONAL_LIGHTS";
            public const string AdditionalLightsVertex = "_TOON_RP_ADDITIONAL_LIGHTS_VERTEX";

            public static class BuiltIn
            {
                public const string DirLightmapCombined = "DIRLIGHTMAP_COMBINED";
                public const string LightmapOn = "LIGHTMAP_ON";
            }
        }
    }
}