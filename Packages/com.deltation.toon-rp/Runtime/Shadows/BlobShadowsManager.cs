using System.Collections.Generic;

namespace DELTation.ToonRP.Shadows
{
    internal static class BlobShadowsManager
    {
        public static HashSet<BlobShadowRenderer> Renderers { get; } = new();

        public static void OnRendererEnabled(BlobShadowRenderer blobShadowRenderer)
        {
            Renderers.Add(blobShadowRenderer);
        }

        public static void OnRendererDisabled(BlobShadowRenderer blobShadowRenderer)
        {
            Renderers.Remove(blobShadowRenderer);
        }
    }
}