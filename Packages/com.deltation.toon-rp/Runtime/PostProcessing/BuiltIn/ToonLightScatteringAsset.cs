using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [CreateAssetMenu(menuName = Path + "Light Scattering")]
    public class ToonLightScatteringAsset : ToonPostProcessingPassAsset<ToonLightScatteringSettings>
    {
        private void Reset()
        {
            Settings = new ToonLightScatteringSettings
            {
                Intensity = 0.25f,
                Threshold = 0.8f,
                ResolutionFactor = 4,
                Samples = 100,
                BlurWidth = 0.8f,
            };
        }

        public override int Order() => ToonPostProcessingPassOrders.LightScattering;

        public override IToonPostProcessingPass CreatePass() => new ToonLightScattering();

        protected override string[] ForceIncludedShaderNames() => new[]
        {
            ToonLightScattering.ShaderName,
        };
    }
}