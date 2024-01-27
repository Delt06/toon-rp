using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace DELTation.ToonRP.Xr
{
    public static class ToonXr
    {
        public const string OcclusionMeshShaderName = "Hidden/Toon RP/XR/XROcclusionMesh";
        public const string MirrorViewShaderName = "Hidden/Toon RP/XR/XRMirrorView";

        public static void BeginXrRendering(ref ScriptableRenderContext context, CommandBuffer cmd, XRPass xrPass)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (xrPass.enabled)
            {
                xrPass.StartSinglePass(cmd);
                context.ExecuteCommandBufferAndClear(cmd);
            }
#endif // ENABLE_VR && ENABLE_XR_MODULE
        }

        public static void EndXrRendering(ref ScriptableRenderContext context, CommandBuffer cmd, XRPass xrPass)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (xrPass.enabled)
            {
                xrPass.StopSinglePass(cmd);
                context.ExecuteCommandBufferAndClear(cmd);
            }
#endif // ENABLE_VR && ENABLE_XR_MODULE
        }

        public static void DrawOcclusionMesh(ref ScriptableRenderContext context, CommandBuffer cmd, XRPass xrPass)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (xrPass.hasValidOcclusionMesh)
            {
                xrPass.RenderOcclusionMesh(cmd);
                context.ExecuteCommandBufferAndClear(cmd);
            }
#endif // ENABLE_VR && ENABLE_XR_MODULE
        }

        public static void UpdateCameraStereoMatrices(Camera camera, XRPass xr)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (xr.enabled)
            {
                if (xr.singlePassEnabled)
                {
                    for (int i = 0; i < Mathf.Min(2, xr.viewCount); i++)
                    {
                        camera.SetStereoProjectionMatrix((Camera.StereoscopicEye) i, xr.GetProjMatrix(i));
                        camera.SetStereoViewMatrix((Camera.StereoscopicEye) i, xr.GetViewMatrix(i));
                    }
                }
                else
                {
                    camera.SetStereoProjectionMatrix((Camera.StereoscopicEye) xr.multipassId, xr.GetProjMatrix());
                    camera.SetStereoViewMatrix((Camera.StereoscopicEye) xr.multipassId, xr.GetViewMatrix());
                }
            }
#endif // ENABLE_VR && ENABLE_XR_MODULE
        }

        public static void EmitErrorOnXr(XRPass xr, string passName)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (xr.enabled)
            {
                Debug.LogError($"{passName} is not supported in XR. Please remove {passName} from XR pipeline assets.");
            }
#endif // ENABLE_VR && ENABLE_XR_MODULE
        }
    }
}