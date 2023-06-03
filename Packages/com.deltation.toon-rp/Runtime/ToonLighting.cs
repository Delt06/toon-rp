using System;
using JetBrains.Annotations;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public sealed class ToonLighting
    {
        private const string CmdName = "Lighting";
        private const int MaxAdditionalLightCount = 64;
        // TODO: implement stripping
        public const string AdditionalLightsGlobalKeyword = "_TOON_RP_ADDITIONAL_LIGHTS";
        private static readonly int DirectionalLightColorId = Shader.PropertyToID("_DirectionalLightColor");
        private static readonly int DirectionalLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");
        private static readonly int AdditionalLightCountId = Shader.PropertyToID("_AdditionalLightCount");
        private static readonly int AdditionalLightColorsId = Shader.PropertyToID("_AdditionalLightColors");
        private static readonly int AdditionalLightPositionsId = Shader.PropertyToID("_AdditionalLightPositions");
        private static GlobalKeyword _additionalLightsGlobalKeyword;
        private readonly Vector4[] _additionalLightColors = new Vector4[MaxAdditionalLightCount];
        private readonly Vector4[] _additionalLightPositions = new Vector4[MaxAdditionalLightCount];

        private readonly CommandBuffer _buffer = new() { name = CmdName };
        private int _additionalLightsCount;

        public ToonLighting() => _additionalLightsGlobalKeyword = GlobalKeyword.Create(AdditionalLightsGlobalKeyword);

        public void Setup(ref ScriptableRenderContext context, ref CullingResults cullingResults,
            in ToonCameraRendererSettings settings,
            [CanBeNull] Light mainLight)
        {
            _buffer.BeginSample(CmdName);
            SetupDirectionalLight(mainLight);

            if (settings.AdditionalLights)
            {
                NativeArray<int> indexMap = cullingResults.GetLightIndexMap(Allocator.Temp);
                SetupAdditionalLights(indexMap, cullingResults.visibleLights);
                cullingResults.SetLightIndexMap(indexMap);
                indexMap.Dispose();
            }

            bool useAdditionalLights = _additionalLightsCount > 0;
            _buffer.SetKeyword(_additionalLightsGlobalKeyword, useAdditionalLights);

            _buffer.EndSample(CmdName);
            context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
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

            _buffer.SetGlobalInteger(AdditionalLightCountId, _additionalLightsCount);
            if (_additionalLightsCount > 0)
            {
                _buffer.SetGlobalVectorArray(AdditionalLightColorsId, _additionalLightColors);
                _buffer.SetGlobalVectorArray(AdditionalLightPositionsId, _additionalLightPositions);
            }
        }

        private void SetupPointLight(int index, in VisibleLight visibleLight)
        {
            _additionalLightColors[index] = visibleLight.finalColor;
            Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
            position.w = 1.0f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
            _additionalLightPositions[index] = position;
        }
    }
}