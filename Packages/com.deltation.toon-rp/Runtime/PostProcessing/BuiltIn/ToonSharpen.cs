using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    public class ToonSharpen : ToonPostProcessingPassBase
    {
        public const string ShaderName = "Hidden/Toon RP/Sharpen";
        private static readonly int AmountId = Shader.PropertyToID("_Amount");
        private readonly ToonPipelineMaterial _material = new(ShaderName, "Toon RP Sharpen");
        private Camera _camera;
        private ToonSharpenSettings _settings;

        public override void Dispose()
        {
            base.Dispose();
            _material.Dispose();
        }

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
                cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                const bool renderToTexture = true;

                if (_camera.cameraType == CameraType.Game)
                {
                    Material material = _material.GetOrCreate();
                    material.SetFloat(AmountId, _settings.Amount);
                    cmd.SetGlobalTexture(ToonBlitter.MainTexId, source);
                    ToonBlitter.Blit(cmd, material, renderToTexture, 0);
                }
                else
                {
                    ToonBlitter.BlitDefault(cmd, source, renderToTexture);
                }
            }
        }
    }
}