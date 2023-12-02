using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using static Unity.Mathematics.math;

namespace DELTation.ToonRP.Shadows.Blobs
{
    public sealed class ToonBlobShadowsCulling
    {
        private static readonly ProfilerMarker Marker =
            new("BlobShadows.Cull");

        private readonly Plane[] _frustumPlanes = new Plane[6];
        private readonly float4[] _frustumPlanesFloat4 = new float4[6];
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

            for (int i = 0; i < _frustumPlanes.Length; i++)
            {
                _frustumPlanesFloat4[i] = float4(_frustumPlanes[i].normal, _frustumPlanes[i].distance);
            }

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
            _bounds.Size *= 1.01f;
        }

        private void CullRenderers(List<BlobShadowRenderer> renderers)
        {
            int i = 0;

            foreach (BlobShadowRenderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                float halfSize = renderer.HalfSize;
                Vector3 position = renderer.Position;
                var positionXZ = new float2(position.x, position.z);
                Bounds2D bounds = ComputeBounds(halfSize, positionXZ);

                if (!AabbInFrustum(bounds))
                {
                    continue;
                }

                if (i == 0)
                {
                    _bounds = bounds;
                }
                else
                {
                    _bounds.Encapsulate(bounds);
                }

                Renderers.Add(new RendererData
                    {
                        Position = positionXZ,
                        HalfSize = halfSize,
                        ShadowType = renderer.ShadowType,
                        Params = renderer.Params,
                        BakedShadowTexture = renderer.ShadowType == BlobShadowType.Baked
                            ? renderer.Baked.BakedShadowTexture
                            : null,
                    }
                );

                i++;
            }
        }

        private bool AabbInFrustum(in Bounds2D bounds)
        {
            const float minY = 0.0f;
            const float maxY = 0.0f;

            // check box outside/inside of frustum
            for (int i = 0; i < 6; i++)
            {
                int sum = 0;
                float4 plane = _frustumPlanesFloat4[i];

                // dot(plane, float4(corner, 1.0))
                sum += plane.x * bounds.Min.x + plane.y * minY + plane.z * bounds.Min.y + plane.w < 0.0 ? 1 : 0;
                sum += plane.x * bounds.Max.x + plane.y * minY + plane.z * bounds.Min.y + plane.w < 0.0 ? 1 : 0;
                sum += plane.x * bounds.Min.x + plane.y * maxY + plane.z * bounds.Min.y + plane.w < 0.0 ? 1 : 0;
                sum += plane.x * bounds.Max.x + plane.y * maxY + plane.z * bounds.Min.y + plane.w < 0.0 ? 1 : 0;
                sum += plane.x * bounds.Min.x + plane.y * minY + plane.z * bounds.Max.y + plane.w < 0.0 ? 1 : 0;
                sum += plane.x * bounds.Max.x + plane.y * minY + plane.z * bounds.Max.y + plane.w < 0.0 ? 1 : 0;
                sum += plane.x * bounds.Min.x + plane.y * maxY + plane.z * bounds.Max.y + plane.w < 0.0 ? 1 : 0;
                sum += plane.x * bounds.Max.x + plane.y * maxY + plane.z * bounds.Max.y + plane.w < 0.0 ? 1 : 0;

                if (sum == 8)
                {
                    return false;
                }
            }

            return true;
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

        private static Bounds2D ComputeBounds(float halfSize, float2 positionXZ)
        {
            float size = halfSize * 2;
            var bounds = new Bounds2D(positionXZ, new float2(size, size));
            return bounds;
        }

        public struct RendererData
        {
            public float2 Position;
            public float HalfSize;
            public BlobShadowType ShadowType;
            public float4 Params;
            public Texture2D BakedShadowTexture;
        }
    }
}