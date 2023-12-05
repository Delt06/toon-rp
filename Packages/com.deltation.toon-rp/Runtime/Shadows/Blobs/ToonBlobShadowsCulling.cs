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

        public List<(ToonBlobShadowsManager manager, ToonBlobShadowType shadowType, NativeList<int> indices)>
            VisibleGroups { get; } = new();

        public void Cull(List<ToonBlobShadowsManager> managers, in Bounds2D receiverBounds)
        {
            using ProfilerMarker.AutoScope profilerScope = Marker.Auto();

            VisibleGroups.Clear();

            foreach (ToonBlobShadowsManager manager in managers)
            {
                for (int typeIndex = 0; typeIndex < ToonBlobShadowTypes.Count; typeIndex++)
                {
                    CullRenderers(manager, (ToonBlobShadowType) typeIndex, receiverBounds);
                }
            }
        }

        public void Clear()
        {
            foreach ((ToonBlobShadowsManager _, ToonBlobShadowType _, NativeList<int> indices) in VisibleGroups)
            {
                indices.Dispose();
            }

            VisibleGroups.Clear();
        }

        private void CullRenderers(ToonBlobShadowsManager manager, ToonBlobShadowType shadowType,
            in Bounds2D receiverBounds)
        {
            ToonBlobShadowsManager.Group group = manager.GetGroup(shadowType);
            if (group.Renderers.Count == 0)
            {
                return;
            }

            using (UpdateRendererDataMarker.Auto())
            {
                group.UpdateRendererData();
            }

            int maxRenderers = group.Renderers.Count;
            var indices = new NativeList<int>(maxRenderers, Allocator.TempJob);

            new ToonBlobShadowsCullingJob
                {
                    Data = group.Data,
                    ReceiverBounds = receiverBounds,
                }
                .ScheduleAppend(indices, maxRenderers)
                .Complete();

            if (indices.Length > 0)
            {
                VisibleGroups.Add((manager, shadowType, indices));
            }
            else
            {
                indices.Dispose();
            }
        }
    }
}