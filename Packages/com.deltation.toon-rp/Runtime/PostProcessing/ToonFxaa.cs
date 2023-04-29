using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing
{
    public class ToonFxaa : ToonPostProcessingPassBase
    {
        private static readonly int FixedContrastThresholdId = Shader.PropertyToID("_FixedContrastThreshold");
        private static readonly int RelativeContrastThresholdId = Shader.PropertyToID("_RelativeContrastThreshold");
        private static readonly int SubpixelBlendingFactorId = Shader.PropertyToID("_SubpixelBlendingFactor");
        private ToonFxaaSettings _fxaaSettings;
        private Material _material;

        public override bool IsEnabled(in ToonPostProcessingSettings settings) => settings.Fxaa.Enabled;

        private void EnsureMaterialIsCreated()
        {
            if (_material != null)
            {
                return;
            }

            _material = new Material(Shader.Find("Hidden/Toon RP/FXAA"))
            {
                name = "Toon RP FXAA",
            };
        }

        public override void Setup(CommandBuffer cmd, in ToonPostProcessingContext context)
        {
            base.Setup(cmd, in context);
            _fxaaSettings = context.Settings.Fxaa;
        }

        private int SelectPass() => _fxaaSettings.HighQuality ? 0 : 1;

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination)
        {
            EnsureMaterialIsCreated();

            if (_fxaaSettings.HighQuality)
            {
                _material.SetFloat(FixedContrastThresholdId, _fxaaSettings.FixedContrastThresholdId);
                _material.SetFloat(RelativeContrastThresholdId, _fxaaSettings.RelativeContrastThreshold);
                _material.SetFloat(SubpixelBlendingFactorId, _fxaaSettings.SubpixelBlendingFactor);
            }

            int pass = SelectPass();

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.Fxaa)))
            {
                cmd.Blit(source, destination, _material, pass);
            }
        }
    }
}