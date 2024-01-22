using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public static class ToonRTHandles
    {
        public static void ReAllocateIfNeeded(this RTHandleSystem rtHandleSystem, [CanBeNull] ref RTHandle rtHandle,
            string name,
            Vector2Int? dimensions = null,
            int arraySlices = 1,
            Vector2? scaleFactor = null,
            GraphicsFormat colorFormat = GraphicsFormat.R8G8B8A8_SRGB,
            int depthBufferBits = 0,
            MSAASamples msaaSamples = MSAASamples.None,
            bool bindTextureMs = false,
            FilterMode filterMode = FilterMode.Point,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat
        )
        {
            bool realloc = false;

            if (rtHandle == null || rtHandle.rt == null)
            {
                realloc = true;
            }
            else
            {
                if (rtHandle.scaleFactor != (scaleFactor ?? Vector2.zero) ||
                    rtHandle.rt.graphicsFormat != colorFormat ||
                    rtHandle.rt.filterMode != filterMode ||
                    rtHandle.rt.wrapMode != wrapMode ||
                    // rtHandle.rt.depth != depthBufferBits ||
                    rtHandle.rt.volumeDepth != arraySlices ||
                    rtHandle.rt.descriptor.msaaSamples != (int) msaaSamples ||
                    rtHandle.rt.bindTextureMS != bindTextureMs
                   )
                {
                    realloc = true;
                }

                if (dimensions.HasValue)
                {
                    if (rtHandle.rt.width != dimensions.Value.x ||
                        rtHandle.rt.height != dimensions.Value.y)
                    {
                        realloc = true;
                    }
                }
            }

            if (realloc)
            {
                rtHandleSystem.Release(rtHandle);

                var depthBits = (DepthBits) depthBufferBits;
                TextureDimension textureDimensions =
                    arraySlices > 1 ? TextureDimension.Tex2DArray : TextureDimension.Tex2D;

                if (dimensions != null)
                {
                    int width = dimensions.Value.x;
                    int height = dimensions.Value.y;
                    rtHandle = rtHandleSystem.Alloc(width, height, arraySlices, depthBits, colorFormat, filterMode,
                        wrapMode, textureDimensions, msaaSamples: msaaSamples, bindTextureMS: bindTextureMs, name: name
                    );
                }
                else
                {
                    rtHandle = rtHandleSystem.Alloc(scaleFactor ?? Vector2.one, arraySlices, depthBits, colorFormat,
                        filterMode, wrapMode, textureDimensions, msaaSamples: msaaSamples, bindTextureMS: bindTextureMs,
                        name: name
                    );
                }
            }
        }

        public static void ReleaseIfAllocated(this RTHandleSystem rtHandleSystem, [CanBeNull] ref RTHandle rtHandle)
        {
            if (rtHandle != null)
            {
                rtHandleSystem.Release(rtHandle);
                rtHandle = null;
            }
        }

        public static void ReAllocateCameraRtIfNeeded(this RTHandleSystem rtHandleSystem,
            [CanBeNull] ref RTHandle rtHandle,
            string name, in ToonCameraRendererSettings cameraRendererSettings)
        {
            const bool ignoreMsaa = true;
            GraphicsFormat format = ToonCameraRenderer.GetRenderTextureColorFormat(cameraRendererSettings, ignoreMsaa);
            rtHandleSystem.ReAllocateIfNeeded(ref rtHandle, name,
                colorFormat: format,
                filterMode: cameraRendererSettings.RenderTextureFilterMode,
                wrapMode: TextureWrapMode.Clamp
            );
        }
    }
}