using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public static class ToonGraphicsDevice
    {
        public static bool SupportsMemorylessMsaa => SystemInfo.graphicsDeviceType is GraphicsDeviceType.Metal;
    }
}