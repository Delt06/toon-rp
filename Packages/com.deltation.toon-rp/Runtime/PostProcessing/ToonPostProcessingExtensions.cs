namespace DELTation.ToonRP.PostProcessing
{
    public static class ToonPostProcessingExtensions
    {
        public static bool InterruptsGeometryRenderPass(
            this ToonPostProcessing postProcessing
        ) =>
            postProcessing.TrueForAny(
                (IToonPostProcessingPass pass, in ToonPostProcessingContext context) =>
                    pass.InterruptsGeometryRenderPass(context)
            );
    }
}