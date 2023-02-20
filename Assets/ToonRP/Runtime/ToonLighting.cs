using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;

namespace ToonRP.Runtime
{
    public sealed class ToonLighting
    {
        private const string CmdName = "Lighting";
        private static readonly int DirectionalLightColorId = Shader.PropertyToID("_DirectionalLightColor");
        private static readonly int DirectionalLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");

        private readonly CommandBuffer _buffer = new() { name = CmdName };

        public void Setup(ScriptableRenderContext context, [CanBeNull] Light light)
        {
            _buffer.BeginSample(CmdName);
            SetupDirectionalLight(light);
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
                _buffer.SetGlobalVector(DirectionalLightColorId, Vector4.one);
                _buffer.SetGlobalVector(DirectionalLightDirectionId, Vector4.one);
            }
        }
    }
}