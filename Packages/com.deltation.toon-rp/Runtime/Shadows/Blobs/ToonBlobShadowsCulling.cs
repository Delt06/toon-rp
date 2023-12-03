using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;

namespace DELTation.ToonRP.Shadows.Blobs
{
    [BurstCompile]
    public struct ToonBlobShadowsCullingJob : IJobFilter
    {
        [ReadOnly]
        public NativeArray<ToonBlobShadowsRendererData> Data;
        public Bounds2D ReceiverBounds;

        public bool Execute(int index)
        {
            Bounds2D bounds = Data[index].Bounds;
            return ReceiverBounds.Intersects(bounds);
        }
    }

    public sealed class ToonBlobShadowsCulling
    {
        private static readonly ProfilerMarker Marker = new("BlobShadows.Cull");
        private static readonly ProfilerMarker UpdateRendererDataMarker = new("BlobShadows.UpdateRendererData");

        public List<(ToonBlobShadowsManager manager, NativeList<int> indices)> VisibleRenderers { get; } = new();

        public void Cull(List<ToonBlobShadowsManager> managers, in Bounds2D receiverBounds)
        {
            using ProfilerMarker.AutoScope profilerScope = Marker.Auto();

            VisibleRenderers.Clear();

            foreach (ToonBlobShadowsManager manager in managers)
            {
                CullRenderers(manager, receiverBounds);
            }
        }

        public void Clear()
        {
            foreach ((ToonBlobShadowsManager _, NativeList<int> indices) in VisibleRenderers)
            {
                indices.Dispose();
            }

            VisibleRenderers.Clear();
        }

        private void CullRenderers(ToonBlobShadowsManager manager, in Bounds2D receiverBounds)
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
                    ReceiverBounds = receiverBounds,
                }
                .ScheduleAppend(indices, maxRenderers)
                .Complete();

            if (indices.Length > 0)
            {
                VisibleRenderers.Add((manager, indices));
            }
            else
            {
                indices.Dispose();
            }
        }
    }
}