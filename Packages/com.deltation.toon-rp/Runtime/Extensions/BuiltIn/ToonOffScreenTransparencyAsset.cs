using UnityEngine;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    [CreateAssetMenu(menuName = Path + "Off-Screen Transparency")]
    public class ToonOffScreenTransparencyAsset : ToonRenderingExtensionAsset<ToonOffScreenTransparencySettings>
    {
        public override ToonRenderingEvent Event => Settings.AfterTransparent
            ? ToonRenderingEvent.AfterTransparent
            : ToonRenderingEvent.BeforeTransparent;

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

        public override IToonRenderingExtension CreateExtension() => new ToonOffScreenTransparency();

        protected override string[] ForceIncludedShaderNames() => new[]
        {
            ToonOffScreenTransparency.ShaderName,
            ToonDepthDownsample.ShaderName,
        };

        public override DepthPrePassMode RequiredDepthPrePassMode() =>
            Settings.DepthMode == ToonOffScreenTransparencySettings.DepthRenderMode.Downsample
                ? DepthPrePassMode.Depth
                : DepthPrePassMode.Off;
    }
}