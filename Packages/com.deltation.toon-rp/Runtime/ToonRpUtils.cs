using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public static class ToonRpUtils
    {
        public static void ExecuteCommandBufferAndClear(ref this ScriptableRenderContext context, CommandBuffer cmd)
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        public static void SetViewAndProjectionMatrices(CommandBuffer cmd, Matrix4x4 viewMatrix,
            Matrix4x4 gpuProjectionMatrix,
            bool setInverseMatrices)
        {
            Matrix4x4 viewAndProjectionMatrix = gpuProjectionMatrix * viewMatrix;
            cmd.SetGlobalMatrix(ShaderPropertyId.ViewMatrix, viewMatrix);
            cmd.SetGlobalMatrix(ShaderPropertyId.ProjectionMatrix, gpuProjectionMatrix);
            cmd.SetGlobalMatrix(ShaderPropertyId.ViewAndProjectionMatrix, viewAndProjectionMatrix);

            if (!setInverseMatrices)
            {
                return;
            }

            SetInverseViewAndProjectionMatrices(cmd, viewMatrix, gpuProjectionMatrix, viewAndProjectionMatrix);
        }

        public static void SetupCameraProperties(ref ScriptableRenderContext context, CommandBuffer cmd,
            ToonAdditionalCameraData additionalCameraData,
            Matrix4x4 overrideProjectionMatrix,
            bool renderIntoTexture)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            XRPass xrPass = additionalCameraData.XrPass;
            if (xrPass.enabled)
            {
                cmd.SetViewProjectionMatrices(additionalCameraData.GetViewMatrix(),
                    additionalCameraData.GetProjectionMatrix()
                );
                if (xrPass.singlePassEnabled)
                {
                    for (int viewId = 0; viewId < xrPass.viewCount; viewId++)
                    {
                        XRBuiltinShaderConstants.UpdateBuiltinShaderConstants(
                            additionalCameraData.GetViewMatrix(viewId),
                            additionalCameraData.GetProjectionMatrix(viewId),
                            CorrectRenderIntoTexture(renderIntoTexture),
                            viewId
                        );
                    }

                    XRBuiltinShaderConstants.SetBuiltinShaderConstants(cmd);
                }

                context.ExecuteCommandBufferAndClear(cmd);
            }
#endif // ENABLE_VR && ENABLE_XR_MODULE
            additionalCameraData.SetCustomProjectionMatrix(overrideProjectionMatrix);
            context.SetupCameraProperties(additionalCameraData.Camera);
        }

        private static void SetInverseViewAndProjectionMatrices(CommandBuffer cmd,
            Matrix4x4 viewMatrix,
            Matrix4x4 gpuProjectionMatrix,
            Matrix4x4 viewAndProjectionMatrix
        )
        {
            var inverseViewMatrix = Matrix4x4.Inverse(viewMatrix);
            var inverseProjectionMatrix = Matrix4x4.Inverse(gpuProjectionMatrix);
            var inverseViewProjection = Matrix4x4.Inverse(viewAndProjectionMatrix);
            cmd.SetGlobalMatrix(ShaderPropertyId.InverseViewMatrix, inverseViewMatrix);
            cmd.SetGlobalMatrix(ShaderPropertyId.InverseProjectionMatrix, inverseProjectionMatrix);
            cmd.SetGlobalMatrix(ShaderPropertyId.InverseViewAndProjectionMatrix, inverseViewProjection);
        }

        public static Matrix4x4 GetGPUProjectionMatrix(Matrix4x4 projectionMatrix, bool renderIntoTexture) =>
            GL.GetGPUProjectionMatrix(projectionMatrix, CorrectRenderIntoTexture(renderIntoTexture));

        private static bool CorrectRenderIntoTexture(bool renderIntoTexture) =>
            SystemInfo.graphicsUVStartsAtTop && renderIntoTexture;

        public static Vector4 BuildRampVectorFromEdges(float edge1, float edge2) =>
            BuildRampVectorFromSmoothness(edge1, edge2 - edge1);

        public static Vector4 BuildRampVectorCrispAntiAliased(float edge1) => new(edge1, 0);

        public static Vector4 BuildRampVectorFromSmoothness(float threshold, float smoothness)
        {
            // saturate((v - a) * invBMinusA)
            float invBMinusA = 1.0f / Mathf.Max(smoothness, 0.0001f);
            // Transforming to the mad-compatible form (a * x + b): 
            // saturate(v * invBMinusA - a * invBMinusA) =>
            // saturate(v * invBMinusA + (-a * invBMinusA)) 
            return new Vector4(invBMinusA, -threshold * invBMinusA);
        }

        // Use all slices. The default uses only the first one.
        public static RenderTargetIdentifier FixupTextureArrayIdentifier(RenderTargetIdentifier id) =>
            new(id, 0, CubemapFace.Unknown, -1);

        public static class ShaderPropertyId
        {
            public static readonly int ViewMatrix = Shader.PropertyToID("unity_MatrixV");
            public static readonly int ProjectionMatrix = Shader.PropertyToID("glstate_matrix_projection");
            public static readonly int ViewAndProjectionMatrix = Shader.PropertyToID("unity_MatrixVP");

            public static readonly int InverseViewMatrix = Shader.PropertyToID("unity_MatrixInvV");
            public static readonly int InverseProjectionMatrix = Shader.PropertyToID("unity_MatrixInvP");
            public static readonly int InverseViewAndProjectionMatrix = Shader.PropertyToID("unity_MatrixInvVP");
        }
    }
}