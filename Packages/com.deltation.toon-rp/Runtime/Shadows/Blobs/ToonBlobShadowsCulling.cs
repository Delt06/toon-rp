using System.Collections.Generic;
using Unity.Profiling;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DELTation.ToonRP.Shadows.Blobs
{
    public sealed class ToonBlobShadowsCulling
    {
        private static readonly ProfilerMarker Marker =
            new("BlobShadows.Cull");

        private readonly Plane[] _frustumPlanes = new Plane[6];
        private Bounds2D _bounds;

        public Bounds2D Bounds => _bounds;

        public List<RendererData> Renderers { get; } = new();

        public void Cull(Camera camera, float maxDistance)
        {
            using ProfilerMarker.AutoScope profilerScope = Marker.Auto();

            Renderers.Clear();
            _bounds = new Bounds2D();

            Matrix4x4 worldToProjectionMatrix =
                ComputeCustomProjectionMatrix(camera, maxDistance) * camera.worldToCameraMatrix;
            GeometryUtility.CalculateFrustumPlanes(worldToProjectionMatrix, _frustumPlanes);

            if (camera.cameraType == CameraType.Game)
            {
                List<BlobShadowRenderer> renderers = BlobShadowsManager.GetRenderers(camera);

                if (renderers != null)
                {
                    CullRenderers(renderers);
                }
            }
            else if (camera.cameraType == CameraType.SceneView)
            {
                foreach (BlobShadowsManager manager in BlobShadowsManager.AllManagers)
                {
                    CullRenderers(manager.Renderers);
                }
            }

            // slight padding to ensure shadows do not touch the shadowmap bounds
            // otherwise, there may be artifacts on low resolutions (< 128) 
            _bounds.Size *= Vector2.one * 1.01f;
        }

        private void CullRenderers(List<BlobShadowRenderer> renderers)
        {
            foreach (BlobShadowRenderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                float halfSize = renderer.HalfSize;
                Vector3 position = renderer.Position;
                Bounds2D bounds = ComputeBounds(halfSize, position);

#if UNITY_EDITOR
                PrefabStage currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (currentPrefabStage != null && currentPrefabStage.IsPartOfPrefabContents(renderer.gameObject))
                {
                    continue;
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
                        BakedShadowTexture = renderer.ShadowType == BlobShadowType.Baked
                            ? renderer.Baked.BakedShadowTexture
                            : null,
                    }
                );
            }
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
            public Texture2D BakedShadowTexture;
        }
    }
}