using UnityEngine;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    [CreateAssetMenu(menuName = Path + "Fake Additional Lights")]
    public class ToonFakeAdditionalLightsAsset : ToonRenderingExtensionAsset<ToonFakeAdditionalLightsSettings>
    {
        public override ToonRenderingEvent Event => ToonRenderingEvent.BeforeGeometryPasses;

        private void Reset()
        {
            Settings.Size = TextureSize._512;

            Settings.Threshold = 0.0f;
            Settings.Smoothness = 1.0f;

            Settings.MaxDistance = 100.0f;
            Settings.DistanceFade = 1.0f;

            Settings.ReceiverPlaneY = 0.0f;
            Settings.MaxHeight = 10.0f;
            Settings.HeightFade = 1.0f;
        }

        public override IToonRenderingExtension CreateExtension() => new ToonFakeAdditionalLights();

        protected override string[] ForceIncludedShaderNames() => new[] { ToonFakeAdditionalLights.ShaderName };
    }
}