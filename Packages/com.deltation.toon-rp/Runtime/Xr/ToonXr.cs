using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

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
    }
}