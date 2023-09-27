using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    public class ToonDebugPass : ToonPostProcessingPassBase
    {
        public const string ShaderName = "Hidden/Toon RP/Debug Pass";

        private readonly Material _material = ToonRpUtils.CreateEngineMaterial(ShaderName, "Toon RP Debug Pass");
        private ToonDebugPassSettings _settings;

        public override bool IsEnabled(in ToonPostProcessingSettings settings)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return settings.Find<ToonDebugPassSettings>().Mode != ToonDebugPassSettings.DebugMode.None;
#else
            return false;
#endif
        }

        public override void Setup(CommandBuffer cmd, in ToonPostProcessingContext context)
        {
            base.Setup(cmd, in context);
            _settings = context.Settings.Find<ToonDebugPassSettings>();
        }

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination)
        {
            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.Debug)))
            {
                int passIndex = (int) _settings.Mode - 1;
                cmd.Blit(source, destination, _material, passIndex);
            }
        }
    }
}