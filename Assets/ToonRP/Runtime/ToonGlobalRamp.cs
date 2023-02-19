using UnityEngine;
using UnityEngine.Rendering;

namespace ToonRP.Runtime
{
    public sealed class ToonGlobalRamp
    {
        private const string BufferName = "Global Ramp";
        private static readonly int GlobalRampId = Shader.PropertyToID("_ToonRP_GlobalRamp");
        private static readonly int GlobalRampSpecularId = Shader.PropertyToID("_ToonRP_GlobalRampSpecular");

        private readonly CommandBuffer _buffer = new() { name = BufferName };
        private readonly GlobalKeyword _globalRampCrispKeyword = GlobalKeyword.Create("_TOON_RP_GLOBAL_RAMP_CRISP");

        public void Setup(ScriptableRenderContext context, in ToonRampSettings rampSettings)
        {
            _buffer.BeginSample(BufferName);
            SetupGlobalRamp(rampSettings);
            _buffer.EndSample(BufferName);
            context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
        }

        private void SetupGlobalRamp(in ToonRampSettings rampSettings)
        {
            // diffuse
            {
                float edge1 = rampSettings.Threshold;
                float edge2 = edge1 + rampSettings.Smoothness;
                _buffer.SetGlobalVector(GlobalRampId, new Vector4(edge1, edge2));
            }

            // specular
            {
                float edge1 = rampSettings.SpecularThreshold;
                float edge2 = edge1 + rampSettings.SpecularSmoothness;
                _buffer.SetGlobalVector(GlobalRampSpecularId, new Vector4(edge1, edge2));
            }

            _buffer.SetKeyword(_globalRampCrispKeyword, rampSettings.CrispAntiAliased);
        }
    }
}