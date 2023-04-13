using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public static class GraphicsApiUtils
    {
        // OpenGL uses the [-1, 1] depth clip space.
        // Other APIs use the [0, 1] range. 
        public static bool OpenGlStyleClipDepth
        {
            get
            {
#if UNITY_WEBGL
                return true;
#else // !UNITY_WEBGL
                GraphicsDeviceType api = SystemInfo.graphicsDeviceType;
                return api is
                    GraphicsDeviceType.OpenGLCore or
                    GraphicsDeviceType.OpenGLES2 or
                    GraphicsDeviceType.OpenGLES3;
#endif // UNITY_WEBGL
            }
        }
    }
}