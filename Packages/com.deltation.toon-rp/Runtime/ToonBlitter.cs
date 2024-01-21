using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public static class ToonBlitter
    {
        public const string DefaultBlitShaderPath = "Hidden/Toon RP/Blit";

        private const int SubmeshIndex = 0;
        public static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
        private static Mesh _triangleMesh;
        private static readonly ToonPipelineMaterial DefaultBlitMaterial = new(DefaultBlitShaderPath, "Toon RP Blit");
        
        public static void SetBlitScaleBias(CommandBuffer cmd, Vector4 scaleBias)
        {
            cmd.SetGlobalVector(BlitScaleBiasId, scaleBias);
        }

        public static void Blit(CommandBuffer cmd, Material material, int shaderPass = 0)
        {
            EnsureMeshIsInitialized();
            cmd.DrawMesh(_triangleMesh, Matrix4x4.identity, material, SubmeshIndex, shaderPass);
        }

        public static void BlitDefault(CommandBuffer cmd, RenderTargetIdentifier source)
        {
            EnsureMeshIsInitialized();
            cmd.SetGlobalTexture(MainTexId, source);
            cmd.DrawMesh(_triangleMesh, Matrix4x4.identity, DefaultBlitMaterial.GetOrCreate(), SubmeshIndex);
        }

        private static void EnsureMeshIsInitialized()
        {
            if (_triangleMesh != null)
            {
                return;
            }

            {
                float nearClipZ = -1;
                if (SystemInfo.usesReversedZBuffer)
                {
                    nearClipZ = 1;
                }

                if (_triangleMesh == null)
                {
                    _triangleMesh = new Mesh
                    {
                        vertices = GetFullScreenTriangleVertexPosition(nearClipZ),
                        uv = GetFullScreenTriangleTexCoord(),
                        triangles = new[] { 0, 1, 2 },
                    };
                }

                static Vector3[] GetFullScreenTriangleVertexPosition(float z /*= UNITY_NEAR_CLIP_VALUE*/)
                {
                    var r = new Vector3[3];
                    for (int i = 0; i < 3; i++)
                    {
                        var uv = new Vector2((i << 1) & 2, i & 2);
                        r[i] = new Vector3(uv.x * 2.0f - 1.0f, uv.y * 2.0f - 1.0f, z);
                    }

                    return r;
                }

                static Vector2[] GetFullScreenTriangleTexCoord()
                {
                    var r = new Vector2[3];
                    for (int i = 0; i < 3; i++)
                    {
                        if (SystemInfo.graphicsUVStartsAtTop)
                        {
                            r[i] = new Vector2((i << 1) & 2, 1.0f - (i & 2));
                        }
                        else
                        {
                            r[i] = new Vector2((i << 1) & 2, i & 2);
                        }
                    }

                    return r;
                }
            }
        }
    }
}