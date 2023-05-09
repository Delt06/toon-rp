using System;
using DELTation.ToonRP.PostProcessing.BuiltIn;

namespace DELTation.ToonRP.PostProcessing
{
    [Serializable]
    public struct ToonPostProcessingSettings
    {
        public bool Enabled;
        public ToonPostProcessingPassAsset[] Passes;
        public ToonOutlineSettings Outline;
    }

    public static class ToonPostProcessingSettingsExt
    {
        public static TSettings Find<TSettings>(this in ToonPostProcessingSettings settings)
        {
            foreach (ToonPostProcessingPassAsset passAsset in settings.Passes)
            {
                if (passAsset is ToonPostProcessingPassAsset<TSettings> casedPassAsset)
                {
                    return casedPassAsset.Settings;
                }
            }

            throw new ArgumentException($"Could not find pass {typeof(TSettings)}.");
        }
    }
}