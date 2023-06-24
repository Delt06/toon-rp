using System;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Editor.VertexColorPaint
{
    internal static class ToonMeshUtility
    {
        private static readonly MethodInfo MethodIntersectRayMesh;

        static ToonMeshUtility()
        {
            Type[] editorTypes = typeof(UnityEditor.Editor).Assembly.GetTypes();

            Type typeHandleUtility = editorTypes.First(t => t.Name == "HandleUtility");
            MethodIntersectRayMesh =
                typeHandleUtility.GetMethod(nameof(IntersectRayMesh), BindingFlags.Static | BindingFlags.NonPublic);
        }

        public static Mesh CopyMesh(Mesh source)
        {
            var outMesh = new Mesh
            {
                name = source.name,
                bounds = source.bounds,
            };

            const MeshUpdateFlags meshUpdateFlags =
                MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds;
            outMesh.indexFormat = source.indexFormat;
            outMesh.SetVertexBufferParams(source.vertexCount, source.GetVertexAttributes());

            // vertices
            for (int i = 0; i < source.vertexBufferCount; i++)
            {
                GraphicsBuffer vertexBuffer = source.GetVertexBuffer(i);
                int totalSize = vertexBuffer.stride * vertexBuffer.count;
                byte[] data = new byte[totalSize];
                vertexBuffer.GetData(data);
                outMesh.SetVertexBufferData(data, 0, 0, totalSize, i, meshUpdateFlags);
                vertexBuffer.Release();
            }

            // indices
            {
                outMesh.subMeshCount = source.subMeshCount;
                GraphicsBuffer indexBuffer = source.GetIndexBuffer();
                int totalSize = indexBuffer.stride * indexBuffer.count;
                byte[] data = new byte[totalSize];
                indexBuffer.GetData(data);
                outMesh.SetIndexBufferParams(indexBuffer.count, source.indexFormat);
                outMesh.SetIndexBufferData(data, 0, 0, totalSize, meshUpdateFlags);
                indexBuffer.Release();
            }

            // Submeshes
            for (int i = 0, currentIndexOffset = 0; i < source.subMeshCount; i++)
            {
                int subMeshIndexCount = (int) source.GetIndexCount(i);
                outMesh.SetSubMesh(i, new SubMeshDescriptor(currentIndexOffset, subMeshIndexCount));
                currentIndexOffset += subMeshIndexCount;
            }

            // Skinning
            {
                NativeArray<byte> bonesPerVertex = source.GetBonesPerVertex();
                NativeArray<BoneWeight1> allBoneWeights = source.GetAllBoneWeights();
                outMesh.SetBoneWeights(bonesPerVertex, allBoneWeights);
                bonesPerVertex.Dispose();
                allBoneWeights.Dispose();

                outMesh.bindposes = source.bindposes;
            }

            return outMesh;
        }

        // Adapted from https://gist.github.com/MattRix/9205bc62d558fef98045
        public static bool IntersectRayMesh(Ray ray, Mesh mesh, Matrix4x4 matrix, out RaycastHit hit)
        {
            object[] parameters = { ray, mesh, matrix, null };
            bool result = (bool) MethodIntersectRayMesh.Invoke(null, parameters);
            hit = (RaycastHit) parameters[3];
            return result;
        }
    }
}