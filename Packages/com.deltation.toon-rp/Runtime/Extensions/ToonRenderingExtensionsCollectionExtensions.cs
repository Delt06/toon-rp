namespace DELTation.ToonRP.Extensions
{
    public static class ToonRenderingExtensionsCollectionExtensions
    {
        public static bool RequireCameraDepthStore(
            this ToonRenderingExtensionsCollection extensionsCollection
        ) =>
            extensionsCollection.TrueForAny(
                (IToonRenderingExtension extension, in ToonRenderingExtensionContext context) =>
                    extension.RequireCameraDepthStore(context)
            );
    }
}