using UnityEngine;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    [CreateAssetMenu]
    public class ToonFakeAdditionalLightsAsset : ToonRenderingExtensionAsset<ToonFakeAdditionalLightsSettings>
    {
        public override ToonRenderingEvent Event => ToonRenderingEvent.BeforeGeometryPasses;

        private void Reset()
        {
            Settings.Size = TextureSize._512;
            Settings.MaxDistance = 100.0f;
            Settings.Threshold = 0.0f;
            Settings.Smoothness = 1.0f;
        }

        public override IToonRenderingExtension CreateExtension() => new ToonFakeAdditionalLights();

        protected override string[] ForceIncludedShaderNames() => new[] { ToonFakeAdditionalLights.ShaderName };
    }
}