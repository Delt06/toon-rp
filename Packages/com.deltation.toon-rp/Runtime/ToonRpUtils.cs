#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define SET_MATERIAL_NAME
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public static class ToonRpUtils
    {
        [Conditional("SET_MATERIAL_NAME")]
        private static void SetMaterialName(Material material, string name)
        {
            material.name = name;
        }

        public static Material CreateEngineMaterial(Shader shader, string materialName)
        {
            Material material = CoreUtils.CreateEngineMaterial(shader);
            SetMaterialName(material, materialName);
            return material;
        }

        public static Material CreateEngineMaterial(string shaderPath, string materialName)
        {
            Material material = CoreUtils.CreateEngineMaterial(shaderPath);
            SetMaterialName(material, materialName);
            return material;
        }

        public static void ExecuteCommandBufferAndClear(ref this ScriptableRenderContext context, CommandBuffer cmd)
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        public static void SetViewAndProjectionMatrices(CommandBuffer cmd, Matrix4x4 viewMatrix,
            Matrix4x4 gpuProjectionMatrix, bool setInverseMatrices)
        {
            Matrix4x4 viewAndProjectionMatrix = gpuProjectionMatrix * viewMatrix;
            cmd.SetGlobalMatrix(ShaderPropertyId.ViewMatrix, viewMatrix);
            cmd.SetGlobalMatrix(ShaderPropertyId.ProjectionMatrix, gpuProjectionMatrix);
            cmd.SetGlobalMatrix(ShaderPropertyId.ViewAndProjectionMatrix, viewAndProjectionMatrix);

            if (!setInverseMatrices)
            {
                return;
            }

            var inverseViewMatrix = Matrix4x4.Inverse(viewMatrix);
            var inverseProjectionMatrix = Matrix4x4.Inverse(gpuProjectionMatrix);
            Matrix4x4 inverseViewProjection = inverseViewMatrix * inverseProjectionMatrix;
            cmd.SetGlobalMatrix(ShaderPropertyId.InverseViewMatrix, inverseViewMatrix);
            cmd.SetGlobalMatrix(ShaderPropertyId.InverseProjectionMatrix, inverseProjectionMatrix);
            cmd.SetGlobalMatrix(ShaderPropertyId.InverseViewAndProjectionMatrix, inverseViewProjection);
        }

        public static Matrix4x4 GetGPUProjectionMatrix(Matrix4x4 projectionMatrix) =>
            GL.GetGPUProjectionMatrix(projectionMatrix, SystemInfo.graphicsUVStartsAtTop);

        public static void RestoreCameraMatrices(Camera camera, CommandBuffer cmd, bool setInverseMatrices)
        {
            Matrix4x4 gpuProjectionMatrix = GetGPUProjectionMatrix(camera.projectionMatrix);
            SetViewAndProjectionMatrices(cmd, camera.worldToCameraMatrix, gpuProjectionMatrix, setInverseMatrices);
        }

        public static Vector4 BuildRampVectorFromEdges(float edge1, float edge2) =>
            BuildRampVectorFromSmoothness(edge1, edge2 - edge1);

        public static Vector4 BuildRampVectorFromSmoothness(float edge1, float smoothness) =>
            new(edge1, 1.0f / Mathf.Max(smoothness, 0.0001f));

        private static class ShaderPropertyId
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