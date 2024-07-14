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
        private ToonSharpenComponent _component;

        public override void Dispose()
        {
            base.Dispose();
            _material.Dispose();
        }

        public override void Setup(CommandBuffer cmd, in ToonPostProcessingContext context)
        {
            base.Setup(cmd, in context);

            _camera = context.Camera;
            _component = GetComponentVolume<ToonSharpenComponent>();
        }

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination, bool destinationIsIntermediateTexture)
        {
            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.Sharpen)))
            {
                cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                Material material = _material.GetOrCreate();
                material.SetFloat(AmountId, _component.Amount.value);
                cmd.SetGlobalTexture(ToonBlitter.MainTexId, source);
                ToonBlitter.Blit(cmd, material, destinationIsIntermediateTexture, 0);
            }
        }
    }
}