using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using static Unity.Mathematics.math;

namespace DELTation.ToonRP.Shadows.Blobs
{
    [BurstCompile]
    public struct ToonBlobShadowsCullingJob : IJobFilter
    {
        [ReadOnly]
        public NativeArray<ToonBlobShadowsRendererData> Data;
        [ReadOnly]
        public NativeArray<float4> FrustumPlanes;
        public float MinY;
        public float MaxY;

        public bool Execute(int index) => AabbInFrustum(Data[index].Bounds);

        private bool AabbInFrustum(in Bounds2D bounds)
        {
            // check box outside/inside of frustum
            for (int i = 0; i < 6; i++)
            {
                int sum = 0;
                float4 plane = FrustumPlanes[i];

                // dot(plane, float4(corner, 1.0))
                sum += plane.x * bounds.Min.x + plane.y * MinY + plane.z * bounds.Min.y + plane.w < 0.0 ? 1 : 0;
                sum += plane.x * bounds.Max.x + plane.y * MinY + plane.z * bounds.Min.y + plane.w < 0.0 ? 1 : 0;
                sum += plane.x * bounds.Min.x + plane.y * MaxY + plane.z * bounds.Min.y + plane.w < 0.0 ? 1 : 0;
                sum += plane.x * bounds.Max.x + plane.y * MaxY + plane.z * bounds.Min.y + plane.w < 0.0 ? 1 : 0;
                sum += plane.x * bounds.Min.x + plane.y * MinY + plane.z * bounds.Max.y + plane.w < 0.0 ? 1 : 0;
                sum += plane.x * bounds.Max.x + plane.y * MinY + plane.z * bounds.Max.y + plane.w < 0.0 ? 1 : 0;
                sum += plane.x * bounds.Min.x + plane.y * MaxY + plane.z * bounds.Max.y + plane.w < 0.0 ? 1 : 0;
                sum += plane.x * bounds.Max.x + plane.y * MaxY + plane.z * bounds.Max.y + plane.w < 0.0 ? 1 : 0;

                if (sum == 8)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public sealed unsafe class ToonBlobShadowsCulling : IDisposable
    {
        private static readonly ProfilerMarker Marker = new("BlobShadows.Cull");
        private static readonly ProfilerMarker UpdateRendererDataMarker = new("BlobShadows.UpdateRendererData");
        private static readonly ProfilerMarker ComputeBoundsMarker = new("BlobShadows.ComputeBounds");

        private readonly Plane[] _frustumPlanes = new Plane[6];
        private Bounds2D _bounds;
        private NativeArray<float4> _frustumPlanesNative =
            new(6, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        private float _minY, _maxY;

        public Bounds2D Bounds => _bounds;

        public List<(ToonBlobShadowsManager manager, NativeList<int> indices)> VisibleRenderers { get; } = new();

        public void Dispose()
        {
            _frustumPlanesNative.Dispose();
        }

        public void Cull(List<ToonBlobShadowsManager> managers, in ToonShadowSettings settings, Camera camera)
        {
            using ProfilerMarker.AutoScope profilerScope = Marker.Auto();

            VisibleRenderers.Clear();
            _bounds = new Bounds2D();

            float maxDistance = Mathf.Min(settings.MaxDistance, camera.farClipPlane);
            Matrix4x4 worldToProjectionMatrix =
                ComputeCustomProjectionMatrix(camera, maxDistance) * camera.worldToCameraMatrix;
            GeometryUtility.CalculateFrustumPlanes(worldToProjectionMatrix, _frustumPlanes);

            for (int i = 0; i < _frustumPlanes.Length; i++)
            {
                _frustumPlanesNative[i] = float4(_frustumPlanes[i].normal, _frustumPlanes[i].distance);
            }

            ref readonly ToonBlobShadowsSettings blobSettings = ref settings.Blobs;
            _minY = blobSettings.ReceiverVolumeY - blobSettings.ReceiverVolumeHeight * 0.5f;
            _maxY = blobSettings.ReceiverVolumeY + blobSettings.ReceiverVolumeHeight * 0.5f;

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
            foreach ((ToonBlobShadowsManager _, NativeList<int> indices) in VisibleRenderers)
            {
                indices.Dispose();
            }

            VisibleRenderers.Clear();
        }

        private void CullRenderers(ToonBlobShadowsManager manager)
        {
            using (UpdateRendererDataMarker.Auto())
            {
                foreach (ToonBlobShadowRenderer dynamicRenderer in manager.DynamicRenderers)
                {
                    if (dynamicRenderer == null)
                    {
                        continue;
                    }

                    dynamicRenderer.GetRendererData();
                }
            }

            int maxRenderers = manager.Renderers.Count;
            var indices = new NativeList<int>(maxRenderers, Allocator.TempJob);

            new ToonBlobShadowsCullingJob
                {
                    Data = manager.Data,
                    FrustumPlanes = _frustumPlanesNative,
                    MinY = _minY,
                    MaxY = _maxY,
                }
                .ScheduleAppend(indices, maxRenderers)
                .Complete();

            using (ComputeBoundsMarker.Auto())
            {
                for (int i = 0; i < indices.Length; i++)
                {
                    ref ToonBlobShadowsRendererData shadowsRendererData = ref manager.DataPtr[indices[i]];
                    if (i == 0)
                    {
                        _bounds = shadowsRendererData.Bounds;
                    }
                    else
                    {
                        _bounds.Encapsulate(shadowsRendererData.Bounds);
                    }
                }
            }

            if (indices.Length > 0)
            {
                VisibleRenderers.Add((manager, indices));
            }
            else
            {
                indices.Dispose();
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
    }
}