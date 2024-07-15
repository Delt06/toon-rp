using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    public class ToonScreenSpaceOutline : ToonPostProcessingPassBase
    {
        private readonly ToonScreenSpaceOutlineImpl _impl = new();
        private ToonScreenSpaceOutlineSettings _settings;
        private ToonScreenSpaceOutlineComponent _component;

        public override void Setup(CommandBuffer cmd, in ToonPostProcessingContext context)
        {
            base.Setup(cmd, in context);
            _settings = context.Settings.Find<ToonScreenSpaceOutlineSettings>();
            _component = GetComponentVolume<ToonScreenSpaceOutlineComponent>();
        }

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination, bool destinationIsIntermediateTexture)
        {
            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.ScreenSpaceOutlines)))
            {
                cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                UpdateSettingsFromComponent();
                _impl.RenderViaBlit(cmd, _settings, destinationIsIntermediateTexture, source);
            }
        }

        public static PrePassMode RequiredPrePassMode(in ToonScreenSpaceOutlineSettings settings)
            => settings.NormalsFilter.Enabled
                ? PrePassMode.Normals | PrePassMode.Depth
                : PrePassMode.Depth;

        private void UpdateSettingsFromComponent()
        {

            _settings.Color = _component.OutlineColor.value;
            _settings.MaxDistance = _component.MaxDistance.value;
            _settings.DistanceFade = _component.DistanceFade.value;
            _settings.UseFog = _component.UseFog.value;
            
            _settings.DepthFilter.Threshold = _component.DepthFilter.value.Threshold;
            _settings.DepthFilter.Smoothness = _component.DepthFilter.value.Smoothness;

            _settings.NormalsFilter.Threshold = _component.NormalsFilter.value.Threshold;
            _settings.NormalsFilter.Smoothness = _component.NormalsFilter.value.Smoothness;

            _settings.ColorFilter.Threshold = _component.ColorFilter.value.Threshold;
            _settings.ColorFilter.Smoothness = _component.ColorFilter.value.Smoothness;

            _settings.Color = _component.OutlineColor.value;
            
        }
    }
}