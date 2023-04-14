namespace DELTation.ToonRP.PostProcessing
{
    public static class ToonPostProcessingSettingsExt
    {
        public static bool HasFullScreenEffects(this in ToonPostProcessingSettings settings)
        {
            if (!settings.Enabled)
            {
                return false;
            }

            if (settings.Bloom.Enabled)
            {
                return true;
            }

            return settings.Outline.Mode == ToonOutlineSettings.OutlineMode.ScreenSpace;
        }
    }
}