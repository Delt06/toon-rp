namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    public static class ToonPostProcessingPassOrders
    {
        public const int Outline = 100;
        public const int MotionBlur = 300;
        public const int LightScattering = 450;
        public const int Bloom = 500;
        public const int TemporalAA = 750;
        public const int SharpenPreUpscale = 900;
        public const int PostProcessingStackPreUpscale = 1000;

        public const int SwitchToNativeResolution = 5000;

        public const int SharpenPostUpscale = 5900;
        public const int PostProcessingStackPostUpscale = 6000;
        public const int Debug = 1_000_000;
    }
}