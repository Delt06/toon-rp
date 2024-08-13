using UnityEngine;
using UnityEngine.Rendering;
using static DELTation.ToonRP.ToonRpUtils;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    public class ToonMotionBlur : ToonPostProcessingPassBase
    {
        public const string ShaderName = "Hidden/Toon RP/Motion Blur";
        private static readonly int NumSamplesId = Shader.PropertyToID("_ToonRP_MotionBlur_NumSamples");
        private static readonly int StrengthId = Shader.PropertyToID("_ToonRP_MotionBlur_Strength");
        private static readonly int TargetFpsId = Shader.PropertyToID("_ToonRP_MotionBlur_TargetFPS");
        private static readonly int WeightChangeRateId = Shader.PropertyToID("_ToonRP_MotionBlur_WeightChangeRate");

        private readonly ToonPipelineMaterial _material = new(ShaderName, "Toon RP Motion Blur");
        private Camera _camera;
        private ToonMotionBlurSettings _settings;
        private ToonMotionBlurComponent _component;

        public override void Setup(CommandBuffer cmd, in ToonPostProcessingContext context)
        {
            base.Setup(cmd, in context);
            _camera = context.Camera;
            _settings = context.Settings.Find<ToonMotionBlurSettings>();
            _component = GetComponentVolume<ToonMotionBlurComponent>();
        }

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, bool destinationIsIntermediateTexture)
        {
            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.MotionBlur)))
            {
                cmd.SetRenderTarget(FixupTextureArrayIdentifier(destination), RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store
                );

                if (_camera.cameraType == CameraType.Game)
                {
                    cmd.SetGlobalTexture(ToonBlitter.MainTexId, FixupTextureArrayIdentifier(source));
                    cmd.SetGlobalFloat(StrengthId, _component.Strength.value);
                    cmd.SetGlobalFloat(NumSamplesId, _component.NumSamples.value);
                    cmd.SetGlobalFloat(WeightChangeRateId, _settings.WeightChangeRate);
                    cmd.SetGlobalFloat(TargetFpsId, ToonMotionBlurSettings.TargetFPS);
                    ToonBlitter.Blit(cmd, _material.GetOrCreate(), destinationIsIntermediateTexture, 0);
                }
                else
                {
                    ToonBlitter.BlitDefault(cmd, source, destinationIsIntermediateTexture);
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _material.Dispose();
        }
    }
}