using System;

namespace DELTation.ToonRP.PostProcessing
{
    [Serializable]
    public struct ToonPostProcessingSettings
    {
        public bool Enabled;
        public ToonPostProcessingPassAsset[] Passes;
    }

    public static class ToonPostProcessingSettingsExt
    {
        public static TSettings Find<TSettings>(this in ToonPostProcessingSettings settings)
        {
            foreach (ToonPostProcessingPassAsset passAsset in settings.Passes)
            {
                if (passAsset is ToonPostProcessingPassAsset<TSettings> castedPassAsset)
                {
                    return castedPassAsset.Settings;
                }
            }

            throw new ArgumentException($"Could not find pass {typeof(TSettings)}.");
        }

        public static bool TryFind<TSettings>(this in ToonPostProcessingSettings settings, out TSettings foundSettings)
        {
            foreach (ToonPostProcessingPassAsset passAsset in settings.Passes)
            {
                if (passAsset is not ToonPostProcessingPassAsset<TSettings> casted)
                {
                    continue;
                }

                foundSettings = casted.Settings;
                return true;
            }

            foundSettings = default;
            return false;
        }
    }
}