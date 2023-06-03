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
        private static readonly int DirectionalLightColorId = Shader.PropertyToID("_DirectionalLightColor");
        private static readonly int DirectionalLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");
        private static readonly int AdditionalLightCountId = Shader.PropertyToID("_AdditionalLightCount");
        private static readonly int AdditionalLightColorsId = Shader.PropertyToID("_AdditionalLightColors");
        private static readonly int AdditionalLightPositionsId = Shader.PropertyToID("_AdditionalLightPositions");
        private readonly Vector4[] _additionalLightColors = new Vector4[MaxAdditionalLightCount];
        private readonly Vector4[] _additionalLightPositions = new Vector4[MaxAdditionalLightCount];

        private readonly CommandBuffer _buffer = new() { name = CmdName };

        public void Setup(ref ScriptableRenderContext context, in CullingResults cullingResults,
            [CanBeNull] Light mainLight)
        {
            _buffer.BeginSample(CmdName);
            SetupDirectionalLight(mainLight);
            SetupAdditionalLights(cullingResults.visibleLights);
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

        private void SetupAdditionalLights(NativeArray<VisibleLight> visibleLights)
        {
            int additionalLightsCount = 0;

            foreach (VisibleLight visibleLight in visibleLights)
            {
                switch (visibleLight.lightType)
                {
                    case LightType.Point:
                        if (additionalLightsCount < MaxAdditionalLightCount)
                        {
                            SetupPointLight(additionalLightsCount, visibleLight);
                            additionalLightsCount++;
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
            }

            _buffer.SetGlobalInteger(AdditionalLightCountId, additionalLightsCount);
            if (additionalLightsCount > 0)
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