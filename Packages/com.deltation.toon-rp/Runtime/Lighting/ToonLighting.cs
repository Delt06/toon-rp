using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using static DELTation.ToonRP.ToonCameraRendererSettings;

namespace DELTation.ToonRP.Lighting
{
    public sealed class ToonLighting
    {
        private const string CmdName = "Lighting";

        private const int MaxAdditionalLightCount = 64;
        // Mirrored with TiledLighting_Shared.hlsl
        public const int MaxAdditionalLightCountTiled = 1024;

        public const string AdditionalLightsGlobalKeyword = "_TOON_RP_ADDITIONAL_LIGHTS";
        public const string AdditionalLightsVertexGlobalKeyword = "_TOON_RP_ADDITIONAL_LIGHTS_VERTEX";
        private static readonly int DirectionalLightColorId = Shader.PropertyToID("_DirectionalLightColor");
        private static readonly int DirectionalLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");
        private static readonly int AdditionalLightCountId = Shader.PropertyToID("_AdditionalLightCount");
        private static readonly int AdditionalLightColorsId = Shader.PropertyToID("_AdditionalLightColors");
        private static readonly int AdditionalLightPositionsId = Shader.PropertyToID("_AdditionalLightPositions");
        private static readonly int AdditionalLightPositionsVsId = Shader.PropertyToID("_AdditionalLightPositionsVS");
        private static GlobalKeyword _additionalLightsGlobalKeyword;
        private static GlobalKeyword _additionalLightsVertexGlobalKeyword;
        private readonly Vector4[] _additionalLightColors = new Vector4[MaxAdditionalLightCount];
        private readonly Vector4[] _additionalLightPositions = new Vector4[MaxAdditionalLightCount];
        private readonly Vector4[] _additionalLightPositionsVs = new Vector4[MaxAdditionalLightCount];

        private readonly CommandBuffer _buffer = new() { name = CmdName };
        private int _additionalLightsCount;
        private TiledLight[] _additionalTiledLights;
        private Vector4[] _additionalTiledLightsColors;
        private Vector4[] _additionalTiledLightsPositionWsAttenuations;
        private Camera _camera;
        private int _currentMaxAdditionalLights;

        public ToonLighting()
        {
            _additionalLightsGlobalKeyword = GlobalKeyword.Create(AdditionalLightsGlobalKeyword);
            _additionalLightsVertexGlobalKeyword = GlobalKeyword.Create(AdditionalLightsVertexGlobalKeyword);
        }

        public void Setup(ref ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults,
            in ToonCameraRendererSettings settings,
            in VisibleLight? mainLight
        )
        {
            _camera = camera;

            _currentMaxAdditionalLights = settings.IsTiledLightingEnabledAndSupported()
                ? MaxAdditionalLightCountTiled
                : MaxAdditionalLightCount;

            if (settings.IsTiledLightingEnabledAndSupported())
            {
                _additionalTiledLights ??= new TiledLight[MaxAdditionalLightCountTiled];
                _additionalTiledLightsColors ??= new Vector4[MaxAdditionalLightCountTiled];
                _additionalTiledLightsPositionWsAttenuations ??= new Vector4[MaxAdditionalLightCountTiled];
            }

            _buffer.BeginSample(CmdName);
            SetupDirectionalLight(mainLight);

            AdditionalLightsMode additionalLightsMode = settings.AdditionalLights;
            if (additionalLightsMode != AdditionalLightsMode.Off)
            {
                NativeArray<int> indexMap = cullingResults.GetLightIndexMap(Allocator.Temp);
                SetupAdditionalLights(indexMap, cullingResults.visibleLights);
                cullingResults.SetLightIndexMap(indexMap);
                indexMap.Dispose();
            }

            SetAdditionalLightsKeywords(additionalLightsMode);

            _buffer.EndSample(CmdName);
            context.ExecuteCommandBufferAndClear(_buffer);
        }

        private void SetupDirectionalLight(in VisibleLight? light)
        {
            if (light != null)
            {
                VisibleLight visibleLight = light.Value;
                _buffer.SetGlobalVector(DirectionalLightColorId, visibleLight.finalColor);

                Vector4 direction = (visibleLight.localToWorldMatrix * Vector3.back).normalized;
                _buffer.SetGlobalVector(DirectionalLightDirectionId, direction);
            }
            else
            {
                _buffer.SetGlobalVector(DirectionalLightColorId, Vector4.zero);
                _buffer.SetGlobalVector(DirectionalLightDirectionId, Vector4.zero);
            }
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
            _buffer.SetKeyword(_additionalLightsGlobalKeyword, enablePerPixel);
            _buffer.SetKeyword(_additionalLightsVertexGlobalKeyword, enablePerVertex);
        }

        private void SetupAdditionalLights(NativeArray<int> indexMap, in NativeArray<VisibleLight> visibleLights)
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
                        if (_additionalLightsCount < _currentMaxAdditionalLights)
                        {
                            newIndex = _additionalLightsCount;
                            SetupPointLight(_additionalLightsCount, visibleLight);
                            _additionalLightsCount++;
                        }

                        break;

                    case LightType.Spot:
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

            _buffer.SetGlobalInt(AdditionalLightCountId, _additionalLightsCount);

            if (_additionalLightsCount > 0)
            {
                _buffer.SetGlobalVectorArray(AdditionalLightColorsId, _additionalLightColors);
                _buffer.SetGlobalVectorArray(AdditionalLightPositionsId, _additionalLightPositions);
                _buffer.SetGlobalVectorArray(AdditionalLightPositionsVsId, _additionalLightPositionsVs);
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

        private void SetupPointLight(int index, in VisibleLight visibleLight)
        {
            Vector4 color = visibleLight.finalColor;

            Vector4 positionWsAttenuation = visibleLight.localToWorldMatrix.GetColumn(3);
            positionWsAttenuation.w = 1.0f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);

            Vector4 positionVsRange =
                _camera.worldToCameraMatrix.MultiplyPoint(visibleLight.localToWorldMatrix.GetColumn(3));
            positionVsRange.w = visibleLight.range;

            if (index < MaxAdditionalLightCount)
            {
                _additionalLightColors[index] = color;
                _additionalLightPositions[index] = positionWsAttenuation;
                _additionalLightPositionsVs[index] = positionVsRange;
            }

            if (_additionalTiledLights != null)
            {
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
    }
}