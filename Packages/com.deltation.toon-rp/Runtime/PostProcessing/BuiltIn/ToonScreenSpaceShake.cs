using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    public class ToonScreenSpaceShake : ToonPostProcessingPassBase
    {
        public const string ShaderName = "Hidden/Toon RP/Screen-Space Shake";
        private static readonly int AmountId = Shader.PropertyToID("_ScreenSpaceShake_Amount");

        private readonly ToonPipelineMaterial _material = new(ShaderName, "Toon RP Screen-Space Shake");
        private ToonScreenSpaceShakePersistentData _persistentData;

        public override void Setup(CommandBuffer cmd, in ToonPostProcessingContext context)
        {
            base.Setup(cmd, in context);
            _persistentData = context.AdditionalCameraData.GetPersistentData<ToonScreenSpaceShakePersistentData>();
        }

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination,
            bool destinationIsIntermediateTexture)
        {
            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.ScreenSpaceShake)))
            {
                cmd.SetGlobalFloat(AmountId, _persistentData.CurrentAmount);
                cmd.SetGlobalTexture(ToonBlitter.MainTexId, source);

                Material material = _material.GetOrCreate();
                const int shaderPass = 0;
                cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                ToonBlitter.Blit(cmd, material, destinationIsIntermediateTexture, shaderPass);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _material.Dispose();
        }
    }
}