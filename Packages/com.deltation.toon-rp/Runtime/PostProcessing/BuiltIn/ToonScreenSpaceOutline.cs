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

            _settings.ColorFilter = _component.ColorFilter.value;
            _settings.Color = _component.OutlineColor.value;
            _settings.MaxDistance = _component.MaxDistance.value;
            _settings.DistanceFade = _component.DistanceFade.value;
            _settings.UseFog = _component.UseFog.value;
            
            _settings.DepthFilter = _component.DepthFilter.value;
            _settings.NormalsFilter = _component.NormalsFilter.value;
            _settings.Color = _component.OutlineColor.value;
            
        }
    }
}