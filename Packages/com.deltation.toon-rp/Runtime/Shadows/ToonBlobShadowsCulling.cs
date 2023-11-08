using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif // UNITY_EDITOR

namespace DELTation.ToonRP.Shadows
{
    public sealed class ToonBlobShadowsCulling
    {
        private readonly Plane[] _frustumPlanes = new Plane[6];
        private Bounds2D _bounds;

        public Bounds2D Bounds => _bounds;

        public List<RendererData> Renderers { get; } = new();

        public void Cull(HashSet<BlobShadowRenderer> renderers, Camera camera, float maxDistance)
        {
            Renderers.Clear();
            _bounds = new Bounds2D();

            Matrix4x4 worldToProjectionMatrix =
                ComputeCustomProjectionMatrix(camera, maxDistance) * camera.worldToCameraMatrix;
            GeometryUtility.CalculateFrustumPlanes(worldToProjectionMatrix, _frustumPlanes);

            foreach (BlobShadowRenderer renderer in renderers)
            {
                float halfSize = renderer.HalfSize;
                Vector3 position = renderer.Position;
                Bounds2D bounds = ComputeBounds(halfSize, position);

#if UNITY_EDITOR
                PrefabStage currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (currentPrefabStage != null && currentPrefabStage.IsPartOfPrefabContents(renderer.gameObject))
                {
                    return;
                }
#endif // UNITY_EDITOR

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
                        Position = new Vector2(position.x, position.z),
                        HalfSize = halfSize,
                        ShadowType = renderer.ShadowType,
                        Params = renderer.Params,
                    }
                );
            }

            // slight padding to ensure shadows do not touch the shadowmap bounds
            // otherwise, there may be artifacts on low resolutions (< 128) 
            _bounds.Size *= Vector2.one * 1.01f;
        }

        private static Matrix4x4 ComputeCustomProjectionMatrix(Camera camera, float farPlane)
        {
            if (!camera.orthographic)
            {
                return Matrix4x4.Perspective(camera.fieldOfView, camera.aspect, camera.nearClipPlane, farPlane);
            }

            float halfSizeV = camera.orthographicSize;
            float halfSizeH = halfSizeV * camera.aspect;
            return Matrix4x4.Ortho(-halfSizeH, halfSizeH, -halfSizeV, halfSizeV, camera.nearClipPlane, farPlane);
        }

        private static Bounds2D ComputeBounds(float halfSize, Vector3 position)
        {
            float size = halfSize * 2;
            var bounds = new Bounds2D(new Vector2(position.x, position.z), new Vector2(size, size));
            return bounds;
        }

        public struct RendererData
        {
            public Vector2 Position;
            public float HalfSize;
            public BlobShadowType ShadowType;
            public Vector4 Params;
        }
    }
}