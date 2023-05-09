using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [CreateAssetMenu(menuName = Path + "FXAA")]
    public class ToonFxaaAsset : ToonPostProcessingPassAsset<ToonFxaaSettings>
    {
        private void Reset()
        {
            Settings = new ToonFxaaSettings
            {
                HighQuality = true,
                FixedContrastThresholdId = 0.0833f,
                RelativeContrastThreshold = 0.166f,
                SubpixelBlendingFactor = 0.75f,
            };
        }

        public override int Order() => ToonPostProcessingPassOrders.Fxaa;

        public override IToonPostProcessingPass CreatePass() => new ToonFxaa();

        protected override string[] ForceIncludedShaderNames() => new[]
        {
            ToonFxaa.ShaderName,
        };
    }
}