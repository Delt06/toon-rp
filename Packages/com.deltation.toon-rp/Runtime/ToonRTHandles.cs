using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public static class ToonRTHandles
    {
        public static void EnsureAllocated(this RTHandleSystem rtHandleSystem, ref RTHandle rtHandle,
            string name,
            Vector2 scaleFactor,
            GraphicsFormat colorFormat = GraphicsFormat.R8G8B8A8_SRGB,
            FilterMode filterMode = FilterMode.Point
        )
        {
            bool realloc = false;

            if (rtHandle == null)
            {
                realloc = true;
            }
            else
            {
                if (rtHandle.scaleFactor != scaleFactor || rtHandle.rt.graphicsFormat != colorFormat ||
                    rtHandle.rt.filterMode != filterMode)
                {
                    realloc = true;
                }
            }

            if (realloc)
            {
                rtHandle?.Release();
                rtHandle = rtHandleSystem.Alloc(scaleFactor,
                    colorFormat: colorFormat, filterMode: filterMode,
                    name: name
                );
            }
        }

        public static void EnsureAllocatedCameraTarget(this RTHandleSystem rtHandleSystem, ref RTHandle rtHandle,
            string name, in ToonCameraRendererSettings cameraRendererSettings, bool ignoreMsaa)
        {
            GraphicsFormat format = ToonCameraRenderer.GetRenderTextureColorFormat(cameraRendererSettings, ignoreMsaa);
            rtHandleSystem.EnsureAllocated(ref rtHandle, name,
                Vector2.one, format, cameraRendererSettings.RenderTextureFilterMode
            );
        }
    }
}