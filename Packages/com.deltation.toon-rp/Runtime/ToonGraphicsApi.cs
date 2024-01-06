using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public static class ToonGraphicsApi
    {
        // OpenGL uses the [-1, 1] depth clip space.
        // Other APIs use the [0, 1] range. 
        public static bool OpenGlStyleClipDepth
        {
            get
            {
#if UNITY_WEBGL
                return true;
#else
                GraphicsDeviceType api = SystemInfo.graphicsDeviceType;
                return api is
                    GraphicsDeviceType.OpenGLCore or
                    GraphicsDeviceType.OpenGLES2 or
                    GraphicsDeviceType.OpenGLES3;
#endif // UNITY_WEBGL
            }
        }

        public static bool SupportsNativeRenderPasses =>
            SystemInfo.graphicsDeviceType is GraphicsDeviceType.Vulkan or GraphicsDeviceType.Metal;

        public static bool SupportsMultisampleDepthResolve()
        {
            // Temporarily disabling depth resolve a driver bug on OSX when using some AMD graphics cards. Temporarily disabling depth resolve on that platform
            // TODO: re-enable once the issue is investigated/fixed
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                return false;
            }

            // Should we also check if the format has stencil and check stencil resolve capability only in that case?
            return SystemInfo.supportsMultisampleResolveDepth && SystemInfo.supportsMultisampleResolveStencil;
        }
    }
}