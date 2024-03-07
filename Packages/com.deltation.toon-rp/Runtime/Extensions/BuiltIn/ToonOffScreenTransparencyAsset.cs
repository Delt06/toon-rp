using UnityEngine;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    [CreateAssetMenu(menuName = Path + "Off-Screen Transparency")]
    public class ToonOffScreenTransparencyAsset : ToonRenderingExtensionAsset<ToonOffScreenTransparencySettings>
    {
        private const ToonRenderingEvent MainRenderingEvent = ToonRenderingEvent.AfterPrepass;

        private void Reset()
        {
            Settings = new ToonOffScreenTransparencySettings
            {
                AfterTransparent = false,
                ResolutionFactor = 2,
                LayerMask = -1,
                Tint = Color.white,
                PatternHorizontalTiling = 10,
            };
        }

        private ToonRenderingEvent GetComposeRenderingEvent() =>
            Settings.AfterTransparent
                ? ToonRenderingEvent.AfterTransparent
                : ToonRenderingEvent.BeforeTransparent;

        public override IToonRenderingExtension CreateExtensionOrDefault(ToonRenderingEvent renderingEvent)
        {
            if (renderingEvent == MainRenderingEvent)
            {
                return new ToonOffScreenTransparencyRender();
            }

            if (renderingEvent == GetComposeRenderingEvent())
            {
                return new ToonOffScreenTransparencyCompose();
            }

            return null;
        }

        protected override string[] ForceIncludedShaderNames() => new[]
        {
            ToonOffScreenTransparencyCompose.ShaderName,
            ToonDepthDownsample.ShaderName,
        };

        public override bool UsesRenderingEvent(ToonRenderingEvent renderingEvent) =>
            renderingEvent == MainRenderingEvent || renderingEvent == GetComposeRenderingEvent();

        public override ToonPrePassRequirement RequiredPrePassMode() =>
            new(Settings.DepthMode == ToonOffScreenTransparencySettings.DepthRenderMode.Downsample
                    ? PrePassMode.Depth
                    : PrePassMode.Off, MainRenderingEvent
            );
    }
}