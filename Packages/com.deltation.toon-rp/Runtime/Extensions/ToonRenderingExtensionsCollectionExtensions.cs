namespace DELTation.ToonRP.Extensions
{
    public static class ToonRenderingExtensionsCollectionExtensions
    {
        public static bool InterruptsGeometryRenderPass(
            this ToonRenderingExtensionsCollection extensionsCollection
        ) =>
            extensionsCollection.TrueForAny(
                (IToonRenderingExtension extension, in ToonRenderingExtensionContext context) =>
                    extension.InterruptsGeometryRenderPass(context)
            );
    }
}