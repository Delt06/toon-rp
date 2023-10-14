using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    public class ToonSharpen : ToonPostProcessingPassBase
    {
        public const string ShaderName = "Hidden/Toon RP/Sharpen";
        private static readonly int AmountId = Shader.PropertyToID("_Amount");
        private readonly Material _material = ToonRpUtils.CreateEngineMaterial(ShaderName, "Toon RP Sharpen");
        private Camera _camera;
        private ToonSharpenSettings _settings;

        public override void Setup(CommandBuffer cmd, in ToonPostProcessingContext context)
        {
            base.Setup(cmd, in context);

            _camera = context.Camera;
            _settings = context.Settings.Find<ToonSharpenSettings>();
        }

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination)
        {
            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.Sharpen)))
            {
                if (_camera.cameraType == CameraType.Game)
                {
                    _material.SetFloat(AmountId, _settings.Amount);
                    cmd.Blit(source, destination, _material);
                }
                else
                {
                    cmd.Blit(source, destination);
                }
            }
        }
    }
}