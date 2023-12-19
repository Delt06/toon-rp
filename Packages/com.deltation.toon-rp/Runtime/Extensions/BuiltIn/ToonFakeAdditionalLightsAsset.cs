using UnityEngine;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    [CreateAssetMenu]
    public class ToonFakeAdditionalLightsAsset : ToonRenderingExtensionAsset<ToonFakeAdditionalLightsSettings>
    {
        public override ToonRenderingEvent Event => ToonRenderingEvent.BeforeGeometryPasses;

        public override IToonRenderingExtension CreateExtension() => new ToonFakeAdditionalLights();

        protected override string[] ForceIncludedShaderNames() => new[] { ToonFakeAdditionalLights.ShaderName };
    }
}