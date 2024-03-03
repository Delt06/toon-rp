using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public abstract class ToonPrePassBase
    {
        protected RenderTargetIdentifier GetTemporaryRT(CommandBuffer cmd,
            ToonAdditionalCameraData additionalCameraData,
            int identifier, RenderTextureDescriptor descriptor, FilterMode filterMode)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            XRPass xrPass = additionalCameraData.XrPass;
            if (xrPass.enabled)
            {
                int arraySize = xrPass.viewCount;
                cmd.GetTemporaryRTArray(identifier, descriptor.width, descriptor.height, arraySize,
                    descriptor.depthBufferBits, filterMode, descriptor.graphicsFormat
                );
                return ToonRpUtils.FixupTextureArrayIdentifier(identifier);
            }
#endif // ENABLE_VR && ENABLE_XR_MODULE

            cmd.GetTemporaryRT(identifier, descriptor, filterMode);
            return identifier;
        }
    }
}