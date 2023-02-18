using UnityEngine;
using UnityEngine.Rendering;

namespace ToonRP.Runtime
{
    public sealed class ToonGlobalRamp
    {
        private const string BufferName = "Global Ramp";
        private static readonly int GlobalRampEdge1Id = Shader.PropertyToID("_ToonRP_GlobalRampEdge1");
        private static readonly int GlobalRampEdge2Id = Shader.PropertyToID("_ToonRP_GlobalRampEdge2");
        private static readonly int GlobalShadowColorId = Shader.PropertyToID("_ToonRP_GlobalShadowColor");

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
            float edge1 = rampSettings.Threshold;
            float edge2 = edge1 + rampSettings.Smoothness;
            _buffer.SetGlobalFloat(GlobalRampEdge1Id, edge1);
            _buffer.SetGlobalFloat(GlobalRampEdge2Id, edge2);
            _buffer.SetGlobalColor(GlobalShadowColorId, rampSettings.ShadowColor);
            _buffer.SetKeyword(_globalRampCrispKeyword, rampSettings.CrispAntiAliased);
        }
    }
}