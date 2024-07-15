using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [CreateAssetMenu(menuName = Path + "Bloom")]
    public class ToonBloomAsset : ToonPostProcessingPassAsset<ToonBloomSettings>
    {
        private void Reset()
        {
            Settings = new ToonBloomSettings
            {
                Intensity = 1.0f,
                Threshold = 0.95f,
                ThresholdKnee = 0.5f,
                MaxIterations = ToonBloom.MaxIterations,
                ResolutionFactor = 2,
                DownsampleLimit = 2,
                Pattern = new ToonBloomSettings.PatternSettings
                {
                    Scale = 350,
                    Power = 2,
                    Multiplier = 4,
                    Smoothness = 0.5f,
                    LuminanceThreshold = 0.1f,
                    DotSizeLimit = 1.0f,
                    Blend = 0.1f,
                    FinalIntensityThreshold = 0.25f,
                },
            };
        }

        public override int Order() => ToonPostProcessingPassOrders.Bloom;

        public override IToonPostProcessingPass CreatePass() => new ToonBloom();

        protected override string[] ForceIncludedShaderNames() => new[]
        {
            ToonBloom.ShaderName,
        };

        public override void CopySettingsToVolumeProfile(VolumeProfile profile)
        {
            ToonBloomComponent component = profile.GetOrAddVolumeComponent<ToonBloomComponent>();

            component.Intensity.value = Settings.Intensity;
            component.Threshold.value = Settings.Threshold;
            component.TresholdKnee.value = Settings.ThresholdKnee;
            component.MaxIterations.value = Settings.MaxIterations;
            component.ResolutionFactor.value = Settings.ResolutionFactor;
            component.DownsampleLimit.value = Settings.DownsampleLimit;

            component.PatternEnabled.value = Settings.Pattern.Enabled;
            component.PatternScale.value = Settings.Pattern.Scale;
            component.PatternPower.value = Settings.Pattern.Power;
            component.PatternMultiplier.value = Settings.Pattern.Multiplier;
            component.PatternSmoothness.value = Settings.Pattern.Smoothness;
            component.PatternLuminanceTreshold.value = Settings.Pattern.LuminanceThreshold;
            component.PatternDotSizeLimit.value = Settings.Pattern.DotSizeLimit;
            component.PatternBlend.value = Settings.Pattern.Blend;
            component.PatternFinalIntensityThreshold.value = Settings.Pattern.FinalIntensityThreshold;

            
        }
    }
}