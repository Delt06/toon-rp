using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Shadows
{
    public class DynamicBlobShadowsMesh
    {
        private const MeshUpdateFlags MeshUpdateFlags = UnityEngine.Rendering.MeshUpdateFlags.DontResetBoneBounds |
                                                        UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds |
                                                        UnityEngine.Rendering.MeshUpdateFlags.DontNotifyMeshUsers;
        private const IndexFormat IndexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
        private readonly List<ushort> _indices = new();
        private readonly VertexAttributeDescriptor[] _vertexAttributes;
        private readonly List<Vertex> _vertices = new();
        private Bounds2D _bounds;
        private Vector2 _inverseWorldSize;
        private Mesh _mesh;

        public DynamicBlobShadowsMesh()
        {
            EnsureMeshIsCreated();
            _vertexAttributes = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float16, 2),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2),
            };
        }

        private void EnsureMeshIsCreated()
        {
            if (_mesh != null)
            {
                return;
            }

            _mesh = new Mesh
            {
                name = "Dynamic Blob Shadows Mesh",
            };
            _mesh.MarkDynamic();
        }

        public Mesh Construct(List<ToonBlobShadowsCulling.RendererData> renderers, Bounds2D bounds)
        {
            _bounds = bounds;

            Prepare();
            FillBuffers(renderers);
            UploadBuffers();

            return _mesh;
        }

        private void Prepare()
        {
            _vertices.Clear();
            _indices.Clear();

            _inverseWorldSize = _bounds.Size;
            _inverseWorldSize.x = 1.0f / _inverseWorldSize.x;
            _inverseWorldSize.y = 1.0f / _inverseWorldSize.y;

            EnsureMeshIsCreated();
        }

        private void FillBuffers(List<ToonBlobShadowsCulling.RendererData> renderers)
        {
            foreach (ToonBlobShadowsCulling.RendererData renderer in renderers)
            {
                int baseVertexIndex = _vertices.Count;

                // vertices
                var position = new Vector2(renderer.Position.x, renderer.Position.z);
                position = WorldToHClip(position);

                AddVertex(new Vector2(-1, -1), position, renderer.Radius);
                AddVertex(new Vector2(1, -1), position, renderer.Radius);
                AddVertex(new Vector2(1, 1), position, renderer.Radius);
                AddVertex(new Vector2(-1, 1), position, renderer.Radius);

                // indices
                AddIndex(baseVertexIndex + 0);
                AddIndex(baseVertexIndex + 1);
                AddIndex(baseVertexIndex + 2);

                AddIndex(baseVertexIndex + 2);
                AddIndex(baseVertexIndex + 3);
                AddIndex(baseVertexIndex + 0);
            }

            Assert.AreEqual(_vertices.Count, renderers.Count * 4);
            Assert.AreEqual(_indices.Count, renderers.Count * 6);
        }

        private void UploadBuffers()
        {
            _mesh.SetVertexBufferParams(_vertices.Count, _vertexAttributes);
            _mesh.SetVertexBufferData(_vertices, 0, 0, _vertices.Count, 0, MeshUpdateFlags);

            _mesh.SetIndexBufferParams(_indices.Count, IndexFormat);
            _mesh.SetIndexBufferData(_indices, 0, 0, _indices.Count, MeshUpdateFlags);

            _mesh.SetSubMesh(0, new SubMeshDescriptor
                {
                    bounds = default,
                    topology = MeshTopology.Triangles,
                    baseVertex = 0,
                    firstVertex = 0,
                    indexCount = _indices.Count,
                    indexStart = 0,
                    vertexCount = _vertices.Count,
                }, MeshUpdateFlags
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddVertex(Vector2 originalVertex, Vector2 translation, float radius)
        {
            float diameter = radius * 2.0f;
            Vector2 scale = _inverseWorldSize * diameter;

            Vector2 resultingVertex = originalVertex * scale + translation;
            _vertices.Add(new Vertex
                {
                    Position = Vector2Half.FromVector2(resultingVertex),
                    UV = Vector2Half.FromVector2(originalVertex),
                }
            );
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
        private void AddIndex(int index)
        {
            _indices.Add((ushort) index);
        }

        private struct Vertex
        {
            // ReSharper disable once NotAccessedField.Local
            public Vector2Half Position;
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
    }
}