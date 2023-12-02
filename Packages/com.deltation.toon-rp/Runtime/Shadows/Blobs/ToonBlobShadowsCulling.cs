using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Mathematics.math;

namespace DELTation.ToonRP.Shadows.Blobs
{
    public sealed class ToonBlobShadowsCulling
    {
        private static readonly ProfilerMarker Marker = new("BlobShadows.Cull");

        private readonly Plane[] _frustumPlanes = new Plane[6];
        private readonly float4[] _frustumPlanesFloat4 = new float4[6];
        private Bounds2D _bounds;
        private float _minY, _maxY;

        public Bounds2D Bounds => _bounds;

        public List<(ToonBlobShadowsManager manager, List<int> indices)> VisibleRenderers { get; } = new();

        public void Cull(List<ToonBlobShadowsManager> managers, in ToonBlobShadowsSettings settings, Camera camera,
            float maxDistance)
        {
            using ProfilerMarker.AutoScope profilerScope = Marker.Auto();

            VisibleRenderers.Clear();
            _bounds = new Bounds2D();

            Matrix4x4 worldToProjectionMatrix =
                ComputeCustomProjectionMatrix(camera, maxDistance) * camera.worldToCameraMatrix;
            GeometryUtility.CalculateFrustumPlanes(worldToProjectionMatrix, _frustumPlanes);

            for (int i = 0; i < _frustumPlanes.Length; i++)
            {
                _frustumPlanesFloat4[i] = float4(_frustumPlanes[i].normal, _frustumPlanes[i].distance);
            }

            _minY = settings.ReceiverVolumeY - settings.ReceiverVolumeHeight * 0.5f;
            _maxY = settings.ReceiverVolumeY + settings.ReceiverVolumeHeight * 0.5f;

            foreach (ToonBlobShadowsManager manager in managers)
            {
                CullRenderers(manager);
            }

            // slight padding to ensure shadows do not touch the shadowmap bounds
            // otherwise, there may be artifacts on low resolutions (< 128) 
            _bounds.Size *= 1.01f;
        }

        public void Clear()
        {
            foreach ((ToonBlobShadowsManager _, List<int> indices) in VisibleRenderers)
            {
                ListPool<int>.Release(indices);
            }

            VisibleRenderers.Clear();
        }

        private void CullRenderers(ToonBlobShadowsManager manager)
        {
            int i = 0;

            List<ToonBlobShadowRenderer> renderers = manager.Renderers;
            List<int> indices = ListPool<int>.Get();

            for (int index = 0; index < renderers.Count; index++)
            {
                ToonBlobShadowRenderer renderer = renderers[index];
                if (renderer == null)
                {
                    continue;
                }

                ref readonly ToonBlobShadowsRendererData shadowsRendererData = ref renderer.GetRendererData();

                if (!AabbInFrustum(shadowsRendererData.Bounds))
                {
                    continue;
                }

                if (i == 0)
                {
                    _bounds = shadowsRendererData.Bounds;
                }
                else
                {
                    _bounds.Encapsulate(shadowsRendererData.Bounds);
                }

                indices.Add(index);
                i++;
            }

            if (indices.Count > 0)
            {
                VisibleRenderers.Add((manager, indices));
            }
            else
            {
                ListPool<int>.Release(indices);
            }
        }

        private bool AabbInFrustum(in Bounds2D bounds)
        {
            // check box outside/inside of frustum
            for (int i = 0; i < 6; i++)
            {
                int sum = 0;
                float4 plane = _frustumPlanesFloat4[i];

                // dot(plane, float4(corner, 1.0))
                sum += plane.x * bounds.Min.x + plane.y * _minY + plane.z * bounds.Min.y + plane.w < 0.0 ? 1 : 0;
                sum += plane.x * bounds.Max.x + plane.y * _minY + plane.z * bounds.Min.y + plane.w < 0.0 ? 1 : 0;
                sum += plane.x * bounds.Min.x + plane.y * _maxY + plane.z * bounds.Min.y + plane.w < 0.0 ? 1 : 0;
                sum += plane.x * bounds.Max.x + plane.y * _maxY + plane.z * bounds.Min.y + plane.w < 0.0 ? 1 : 0;
                sum += plane.x * bounds.Min.x + plane.y * _minY + plane.z * bounds.Max.y + plane.w < 0.0 ? 1 : 0;
                sum += plane.x * bounds.Max.x + plane.y * _minY + plane.z * bounds.Max.y + plane.w < 0.0 ? 1 : 0;
                sum += plane.x * bounds.Min.x + plane.y * _maxY + plane.z * bounds.Max.y + plane.w < 0.0 ? 1 : 0;
                sum += plane.x * bounds.Max.x + plane.y * _maxY + plane.z * bounds.Max.y + plane.w < 0.0 ? 1 : 0;

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
    }
}