using System;
using JetBrains.Annotations;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using static DELTation.ToonRP.ToonCameraRendererSettings;

namespace DELTation.ToonRP.Lighting
{
    public sealed class ToonLighting
    {
        private const string CmdName = "Lighting";
        private const int MaxAdditionalLightCount = 64;
        public const string AdditionalLightsGlobalKeyword = "_TOON_RP_ADDITIONAL_LIGHTS";
        public const string AdditionalLightsVertexGlobalKeyword = "_TOON_RP_ADDITIONAL_LIGHTS_VERTEX";
        private static readonly int DirectionalLightColorId = Shader.PropertyToID("_DirectionalLightColor");
        private static readonly int DirectionalLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");
        private static readonly int AdditionalLightCountId = Shader.PropertyToID("_AdditionalLightCount");
        private static readonly int AdditionalLightColorsId = Shader.PropertyToID("_AdditionalLightColors");
        private static readonly int AdditionalLightPositionsId = Shader.PropertyToID("_AdditionalLightPositions");
        private static GlobalKeyword _additionalLightsGlobalKeyword;
        private static GlobalKeyword _additionalLightsVertexGlobalKeyword;
        private readonly Vector4[] _additionalLightColors = new Vector4[MaxAdditionalLightCount];
        private readonly Vector4[] _additionalLightPositions = new Vector4[MaxAdditionalLightCount];

        private readonly CommandBuffer _buffer = new() { name = CmdName };
        private int _additionalLightsCount;

        public ToonLighting()
        {
            _additionalLightsGlobalKeyword = GlobalKeyword.Create(AdditionalLightsGlobalKeyword);
            _additionalLightsVertexGlobalKeyword = GlobalKeyword.Create(AdditionalLightsVertexGlobalKeyword);
        }

        public void Setup(ref ScriptableRenderContext context, ref CullingResults cullingResults,
            in ToonCameraRendererSettings settings,
            [CanBeNull] Light mainLight)
        {
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

        private void SetupDirectionalLight([CanBeNull] Light light)
        {
            if (light != null)
            {
                _buffer.SetGlobalVector(DirectionalLightColorId, light.color.linear * light.intensity);
                _buffer.SetGlobalVector(DirectionalLightDirectionId, -light.transform.forward);
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

        private void SetupAdditionalLights(NativeArray<int> indexMap, NativeArray<VisibleLight> visibleLights)
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
                        if (_additionalLightsCount < MaxAdditionalLightCount)
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
            }
        }

        private void SetupPointLight(int index, in VisibleLight visibleLight)
        {
            Vector4 packedColor = visibleLight.finalColor;
            packedColor.w = visibleLight.range;
            _additionalLightColors[index] = packedColor;

            Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
            position.w = 1.0f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
            _additionalLightPositions[index] = position;
        }
    }
}