using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public sealed class ToonGlobalRamp
    {
        private const string BufferName = "Global Ramp";
        private static readonly int GlobalRampId = Shader.PropertyToID("_ToonRP_GlobalRamp");
        private static readonly int GlobalRampSpecularId = Shader.PropertyToID("_ToonRP_GlobalRampSpecular");
        private static readonly int GlobalRampRimId = Shader.PropertyToID("_ToonRP_GlobalRampRim");

        private readonly CommandBuffer _cmd = new() { name = BufferName };
        private readonly GlobalKeyword _globalRampCrispKeyword = GlobalKeyword.Create("_TOON_RP_GLOBAL_RAMP_CRISP");

        public void Setup(ScriptableRenderContext context, in ToonRampSettings rampSettings)
        {
            _cmd.BeginSample(BufferName);
            SetupGlobalRamp(rampSettings);
            _cmd.EndSample(BufferName);
            context.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }

        private void SetupGlobalRamp(in ToonRampSettings rampSettings)
        {
            // diffuse
            {
                float edge1 = rampSettings.Threshold;
                float edge2 = edge1 + rampSettings.Smoothness;
                _cmd.SetGlobalVector(GlobalRampId, new Vector4(edge1, edge2));
            }

            // specular
            {
                float edge1 = rampSettings.SpecularThreshold;
                float edge2 = edge1 + rampSettings.SpecularSmoothness;
                _cmd.SetGlobalVector(GlobalRampSpecularId, new Vector4(edge1, edge2));
            }

            // rim
            {
                float edge1 = rampSettings.RimThreshold;
                float edge2 = edge1 + rampSettings.RimSmoothness;
                _cmd.SetGlobalVector(GlobalRampRimId, new Vector4(edge1, edge2));
            }

            _cmd.SetKeyword(_globalRampCrispKeyword, rampSettings.CrispAntiAliased);
        }
    }
}