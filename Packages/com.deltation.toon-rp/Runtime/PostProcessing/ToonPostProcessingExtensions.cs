namespace DELTation.ToonRP.PostProcessing
{
    public static class ToonPostProcessingExtensions
    {
        public static bool RequireCameraDepthStore(
            this ToonPostProcessing postProcessing
        ) =>
            postProcessing.TrueForAny(
                (IToonPostProcessingPass pass, in ToonPostProcessingContext context) =>
                    pass.RequireCameraDepthStore(context)
            );
    }
}