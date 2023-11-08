using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Shadows
{
    public abstract class DynamicBlobShadowsMesh
    {
        protected static readonly VertexAttributeDescriptor[] VertexAttributeDescriptorsDefault =
        {
            new(VertexAttribute.Position, VertexAttributeFormat.Float16, 2),
            new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2),
        };

        protected static readonly VertexAttributeDescriptor[] VertexAttributeDescriptorsParams =
        {
            new(VertexAttribute.Position, VertexAttributeFormat.Float16, 2),
            new(VertexAttribute.Color, VertexAttributeFormat.Float16, 4),
            new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2),
        };

        protected static readonly List<ushort> TempIndices = new();

        public abstract BlobShadowType ShadowType { get; }

        [CanBeNull]
        public abstract Mesh Construct(List<ToonBlobShadowsCulling.RendererData> renderers, Bounds2D bounds);

        public struct Vertex
        {
            // ReSharper disable once NotAccessedField.Local
            public Vector2Half Position;
            // ReSharper disable once NotAccessedField.Local
            public Vector2Half UV;
        }

        public struct VertexParams
        {
            // ReSharper disable once NotAccessedField.Local
            public Vector2Half Position;
            // ReSharper disable once NotAccessedField.Local
            public Vector4Half Params;
            // ReSharper disable once NotAccessedField.Local
            public Vector2Half UV;
        }

        public struct Vector2Half
        {
            // ReSharper disable once NotAccessedField.Local
            public ushort X;
            // ReSharper disable once NotAccessedField.Local
            public ushort Y;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector2Half FromVector2(Vector2 vector) =>
                new()
                {
                    X = Mathf.FloatToHalf(vector.x),
                    Y = Mathf.FloatToHalf(vector.y),
                };
        }

        public struct Vector4Half
        {
            // ReSharper disable once NotAccessedField.Local
            public ushort X;
            // ReSharper disable once NotAccessedField.Local
            public ushort Y;
            // ReSharper disable once NotAccessedField.Local
            public ushort Z;
            // ReSharper disable once NotAccessedField.Local
            public ushort W;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector4Half FromVector4(Vector4 vector) =>
                new()
                {
                    X = Mathf.FloatToHalf(vector.x),
                    Y = Mathf.FloatToHalf(vector.y),
                    Z = Mathf.FloatToHalf(vector.z),
                    W = Mathf.FloatToHalf(vector.w),
                };
        }
    }

    public abstract class DynamicBlobShadowsMesh<TVertex> : DynamicBlobShadowsMesh where TVertex : struct
    {
        private const MeshUpdateFlags MeshUpdateFlags = UnityEngine.Rendering.MeshUpdateFlags.DontResetBoneBounds |
                                                        UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds |
                                                        UnityEngine.Rendering.MeshUpdateFlags.DontNotifyMeshUsers;
        private const IndexFormat IndexFormat = UnityEngine.Rendering.IndexFormat.UInt16;


        private static readonly List<TVertex> TempVertices = new();

        private Bounds2D _bounds;
        private Vector2 _inverseWorldSize;
        private Mesh _mesh;
        private int _renderersCount;

        protected abstract VertexAttributeDescriptor[] VertexAttributeDescriptors { get; }

        private void EnsureMeshIsCreated()
        {
            if (_mesh != null)
            {
                return;
            }

            _mesh = new Mesh
            {
                name = $"Dynamic Blob Shadows Mesh ({ShadowType.ToString()})",
            };
            _mesh.MarkDynamic();
        }

        public override Mesh Construct(List<ToonBlobShadowsCulling.RendererData> renderers, Bounds2D bounds)
        {
            _bounds = bounds;

            Prepare();
            FillBuffers(renderers);

            if (_renderersCount > 0)
            {
                EnsureMeshIsCreated();
                UploadBuffers();
                return _mesh;
            }

            return null;
        }

        private void Prepare()
        {
            TempVertices.Clear();
            TempIndices.Clear();
            _renderersCount = 0;

            _inverseWorldSize = _bounds.Size;
            _inverseWorldSize.x = 1.0f / _inverseWorldSize.x;
            _inverseWorldSize.y = 1.0f / _inverseWorldSize.y;
        }

        private void FillBuffers(List<ToonBlobShadowsCulling.RendererData> renderers)
        {
            _renderersCount = 0;

            BlobShadowType shadowType = ShadowType;

            foreach (ToonBlobShadowsCulling.RendererData renderer in renderers)
            {
                if (renderer.ShadowType != shadowType)
                {
                    continue;
                }

                ++_renderersCount;

                // vertices
                var position = new Vector2(renderer.Position.x, renderer.Position.y);
                position = WorldToHClip(position);

                int baseVertexIndex = TempVertices.Count;
                AddVertex(new Vector2(-1, -1), position, renderer.HalfSize, renderer.Params);
                AddVertex(new Vector2(1, -1), position, renderer.HalfSize, renderer.Params);
                AddVertex(new Vector2(1, 1), position, renderer.HalfSize, renderer.Params);
                AddVertex(new Vector2(-1, 1), position, renderer.HalfSize, renderer.Params);

                // indices
                AddIndex(baseVertexIndex + 0);
                AddIndex(baseVertexIndex + 1);
                AddIndex(baseVertexIndex + 2);

                AddIndex(baseVertexIndex + 2);
                AddIndex(baseVertexIndex + 3);
                AddIndex(baseVertexIndex + 0);
            }
        }

        private void UploadBuffers()
        {
            int vertexCount = TempVertices.Count;
            _mesh.SetVertexBufferParams(vertexCount, VertexAttributeDescriptors);
            _mesh.SetVertexBufferData(TempVertices, 0, 0, vertexCount, 0, MeshUpdateFlags);

            _mesh.SetIndexBufferParams(TempIndices.Count, IndexFormat);
            _mesh.SetIndexBufferData(TempIndices, 0, 0, TempIndices.Count, MeshUpdateFlags);

            _mesh.SetSubMesh(0, new SubMeshDescriptor
                {
                    bounds = default,
                    topology = MeshTopology.Triangles,
                    baseVertex = 0,
                    firstVertex = 0,
                    indexCount = TempIndices.Count,
                    indexStart = 0,
                    vertexCount = vertexCount,
                }, MeshUpdateFlags
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddVertex(Vector2 originalVertex, Vector2 translation, float halfSize, Vector4 @params)
        {
            ComputePositionAndUv(originalVertex, translation, halfSize, out Vector2 position, out Vector2 uv);
            TempVertices.Add(BuildVertex(position, uv, @params));
        }

        protected abstract TVertex BuildVertex(Vector2 position, Vector2 uv, Vector4 @params);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputePositionAndUv(Vector2 originalVertex, Vector2 translation, float halfSize,
            out Vector2 position, out Vector2 uv
        )
        {
            float size = halfSize * 2.0f;
            Vector2 scale = _inverseWorldSize * size;

            position = originalVertex * scale + translation;
            uv = originalVertex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector2 WorldToHClip(Vector2 position)
        {
            Vector2 boundsMin = _bounds.Min;
            Vector2 boundsMax = _bounds.Max;
            float x = InverseLerpUnclamped(boundsMin.x, boundsMax.x, position.x);
            x = (x - 0.5f) * 2.0f;
            float y = InverseLerpUnclamped(boundsMin.y, boundsMax.y, position.y);
            y = (y - 0.5f) * 2.0f;

            if (SystemInfo.graphicsUVStartsAtTop)
            {
                y *= -1.0f;
            }

            return new Vector2(x, y);
        }

        private static float InverseLerpUnclamped(float a, float b, float value) =>
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            a != b ? (value - a) / (b - a) : 0.0f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddIndex(int index)
        {
            TempIndices.Add((ushort) index);
        }
    }
}