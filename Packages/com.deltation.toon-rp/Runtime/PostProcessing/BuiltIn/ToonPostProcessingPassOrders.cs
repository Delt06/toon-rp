namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    public static class ToonPostProcessingPassOrders
    {
        public const int Outline = 100;
        public const int LightScattering = 450;
        public const int Bloom = 500;
        public const int TemporalAA = 750;
        public const int Sharpen = 900;

        public const int SwitchToNativeResolution = 1000;

        public const int PostProcessingStack = 1500;
        public const int Debug = 1_000_000;
    }
}