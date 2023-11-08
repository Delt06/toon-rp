using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Shadows
{
    public class DynamicBlobShadowsMesh
    {
        private const MeshUpdateFlags MeshUpdateFlags = UnityEngine.Rendering.MeshUpdateFlags.DontResetBoneBounds |
                                                        UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds |
                                                        UnityEngine.Rendering.MeshUpdateFlags.DontNotifyMeshUsers;
        private const IndexFormat IndexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
        private static readonly VertexAttributeDescriptor[][] VertexAttributeDescriptors;

        private static readonly List<ushort> TempIndices = new();
        private static readonly List<Vertex> TempVertices = new();
        private static readonly List<VertexParams> TempVerticesParams = new();

        private readonly BlobShadowType _shadowType;
        private Bounds2D _bounds;
        private Vector2 _inverseWorldSize;
        private Mesh _mesh;
        private int _renderersCount;

        static DynamicBlobShadowsMesh()
        {
            VertexAttributeDescriptors = new VertexAttributeDescriptor[BlobShadowTypes.Count][];
            VertexAttributeDescriptors[(int) BlobShadowType.Circle] = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float16, 2),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2),
            };
            VertexAttributeDescriptors[(int) BlobShadowType.Square] = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float16, 2),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float16, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2),
            };
        }

        public DynamicBlobShadowsMesh(BlobShadowType shadowType)
        {
            _shadowType = shadowType;
            EnsureMeshIsCreated();
        }

        private void EnsureMeshIsCreated()
        {
            if (_mesh != null)
            {
                return;
            }

            _mesh = new Mesh
            {
                name = $"Dynamic Blob Shadows Mesh ({_shadowType.ToString()})",
            };
            _mesh.MarkDynamic();
        }

        [CanBeNull]
        public Mesh Construct(List<ToonBlobShadowsCulling.RendererData> renderers, Bounds2D bounds)
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
            TempVerticesParams.Clear();
            TempIndices.Clear();
            _renderersCount = 0;

            _inverseWorldSize = _bounds.Size;
            _inverseWorldSize.x = 1.0f / _inverseWorldSize.x;
            _inverseWorldSize.y = 1.0f / _inverseWorldSize.y;
        }

        private void FillBuffers(List<ToonBlobShadowsCulling.RendererData> renderers)
        {
            _renderersCount = 0;

            foreach (ToonBlobShadowsCulling.RendererData renderer in renderers)
            {
                if (renderer.ShadowType != _shadowType)
                {
                    continue;
                }

                ++_renderersCount;
                int baseVertexIndex;

                // vertices
                var position = new Vector2(renderer.Position.x, renderer.Position.y);
                position = WorldToHClip(position);

                switch (_shadowType)
                {
                    case BlobShadowType.Circle:
                    {
                        baseVertexIndex = TempVertices.Count;
                        AddVertex(new Vector2(-1, -1), position, renderer.HalfSize);
                        AddVertex(new Vector2(1, -1), position, renderer.HalfSize);
                        AddVertex(new Vector2(1, 1), position, renderer.HalfSize);
                        AddVertex(new Vector2(-1, 1), position, renderer.HalfSize);
                        break;
                    }
                    case BlobShadowType.Square:
                    {
                        baseVertexIndex = TempVerticesParams.Count;
                        AddVertexWithParams(new Vector2(-1, -1), position, renderer.HalfSize, renderer.Params);
                        AddVertexWithParams(new Vector2(1, -1), position, renderer.HalfSize, renderer.Params);
                        AddVertexWithParams(new Vector2(1, 1), position, renderer.HalfSize, renderer.Params);
                        AddVertexWithParams(new Vector2(-1, 1), position, renderer.HalfSize, renderer.Params);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

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
            int vertexCount;
            VertexAttributeDescriptor[] vertexAttributeDescriptors = VertexAttributeDescriptors[(int) _shadowType];
            if (TempVertices.Count > 0)
            {
                vertexCount = TempVertices.Count;
                _mesh.SetVertexBufferParams(vertexCount, vertexAttributeDescriptors);
                _mesh.SetVertexBufferData(TempVertices, 0, 0, vertexCount, 0, MeshUpdateFlags);
            }
            else
            {
                vertexCount = TempVerticesParams.Count;
                _mesh.SetVertexBufferParams(vertexCount, vertexAttributeDescriptors);
                _mesh.SetVertexBufferData(TempVerticesParams, 0, 0, vertexCount, 0, MeshUpdateFlags);
            }

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
        private void AddVertex(Vector2 originalVertex, Vector2 translation, float halfSize)
        {
            ComputePositionAndUv(originalVertex, translation, halfSize, out Vector2 position, out Vector2 uv);
            TempVertices.Add(new Vertex
                {
                    Position = Vector2Half.FromVector2(position),
                    UV = Vector2Half.FromVector2(uv),
                }
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddVertexWithParams(Vector2 originalVertex, Vector2 translation, float halfSize, Vector4 @params)
        {
            ComputePositionAndUv(originalVertex, translation, halfSize, out Vector2 position, out Vector2 uv);
            TempVerticesParams.Add(new VertexParams
                {
                    Position = Vector2Half.FromVector2(position),
                    Params = Vector4Half.FromVector4(@params),
                    UV = Vector2Half.FromVector2(uv),
                }
            );
        }

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

        private struct Vertex
        {
            // ReSharper disable once NotAccessedField.Local
            public Vector2Half Position;
            // ReSharper disable once NotAccessedField.Local
            public Vector2Half UV;
        }

        private struct VertexParams
        {
            // ReSharper disable once NotAccessedField.Local
            public Vector2Half Position;
            // ReSharper disable once NotAccessedField.Local
            public Vector4Half Params;
            // ReSharper disable once NotAccessedField.Local
            public Vector2Half UV;
        }

        private struct Vector2Half
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

        private struct Vector4Half
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
}