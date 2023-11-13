using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public static class ToonGraphicsDevice
    {
        public static bool SupportsTransientMsaa =>
            SystemInfo.graphicsDeviceType is GraphicsDeviceType.Vulkan or GraphicsDeviceType.Metal;
    }
}