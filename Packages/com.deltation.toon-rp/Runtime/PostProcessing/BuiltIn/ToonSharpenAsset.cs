using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [CreateAssetMenu(menuName = Path + "Sharpen")]
    public class ToonSharpenAsset : ToonPostProcessingPassAsset<ToonSharpenSettings>
    {
        private void Reset()
        {
            Settings = new ToonSharpenSettings
            {
                Amount = 0.8f,
            };
        }

        public override int Order() => Settings.Order switch
        {
            ToonSharpenSettings.PassOrder.PreUpscale => ToonPostProcessingPassOrders.SharpenPreUpscale,
            ToonSharpenSettings.PassOrder.PostUpscale => ToonPostProcessingPassOrders.SharpenPostUpscale,
            _ => throw new ArgumentOutOfRangeException(),
        };

        public override IToonPostProcessingPass CreatePass() => new ToonSharpen();

        protected override string[] ForceIncludedShaderNames() => new[] { ToonSharpen.ShaderName };

        public override void CopySettingsToVolumeProfile(VolumeProfile profile)
        {
            ToonSharpenComponent component = profile.GetOrAddVolumeComponent<ToonSharpenComponent>();
            component.Amount.value = Settings.Amount;
        }
    }
}