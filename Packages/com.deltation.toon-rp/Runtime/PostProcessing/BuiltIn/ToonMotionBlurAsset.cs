using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [CreateAssetMenu(menuName = Path + "Motion Blur")]
    public class ToonMotionBlurAsset : ToonPostProcessingPassAsset<ToonMotionBlurSettings>
    {
        private void Reset()
        {
            Settings = new ToonMotionBlurSettings
            {
                Strength = 2.0f,
                WeightChangeRate = 0.95f,
                NumSamples = 8,
            };
        }

        public override PrePassMode RequiredPrePassMode() => PrePassMode.MotionVectors;

        public override int Order() => ToonPostProcessingPassOrders.MotionBlur;

        public override IToonPostProcessingPass CreatePass() => new ToonMotionBlur();

        protected override string[] ForceIncludedShaderNames() => new[]
        {
            ToonMotionBlur.ShaderName,
        };

        public override void CopySettingsToVolumeProfile(VolumeProfile profile)
        {
            ToonMotionBlurComponent component = GetOrAddVolumeComponent<ToonMotionBlurComponent>(profile);

            component.Strength.value = Settings.Strength;
            component.NumSamples.value = Settings.NumSamples;

        }
    }
}