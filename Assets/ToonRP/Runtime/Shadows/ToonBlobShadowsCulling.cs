using System.Collections.Generic;
using UnityEngine;

namespace ToonRP.Runtime.Shadows
{
    public sealed class ToonBlobShadowsCulling
    {
        private readonly Plane[] _frustumPlanes = new Plane[6];
        private Bounds2D _bounds;

        public Bounds2D Bounds => _bounds;

        public List<RendererData> Renderers { get; } = new();

        public void Cull(HashSet<BlobShadowRenderer> renderers, Camera camera)
        {
            Renderers.Clear();
            _bounds = new Bounds2D();

            GeometryUtility.CalculateFrustumPlanes(camera, _frustumPlanes);

            foreach (BlobShadowRenderer renderer in renderers)
            {
                float radius = renderer.Radius;
                Vector3 position = renderer.Position;
                Bounds2D bounds = ComputeBounds(radius, position);

                if (!GeometryUtility.TestPlanesAABB(_frustumPlanes, bounds.AsXZ(0.0f, 0.01f)))
                {
                    continue;
                }

                if (Bounds.Size == Vector2.zero)
                {
                    _bounds = bounds;
                }
                else
                {
                    _bounds.Encapsulate(bounds);
                }

                Renderers.Add(new RendererData
                    {
                        Position = position,
                        Radius = radius,
                    }
                );
            }
        }

        private static Bounds2D ComputeBounds(float radius, Vector3 position)
        {
            float diameter = radius * 2;
            var bounds = new Bounds2D(new Vector2(position.x, position.z), new Vector2(diameter, diameter));
            return bounds;
        }

        public struct RendererData
        {
            public Vector3 Position;
            public float Radius;
        }
    }
}