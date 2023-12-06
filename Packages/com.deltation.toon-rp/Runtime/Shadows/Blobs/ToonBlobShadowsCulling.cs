using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Profiling;

namespace DELTation.ToonRP.Shadows.Blobs
{
    [BurstCompile]
    public unsafe struct ToonBlobShadowsCullingJob : IJobFilter
    {
        [NativeDisableUnsafePtrRestriction] [ReadOnly]
        public ToonBlobShadowsRendererData* Data;

        public Bounds2D ReceiverBounds;

        public int BaseIndex;

        public bool Execute(int index)
        {
            ref readonly Bounds2D bounds = ref Data[BaseIndex + index].Bounds;
            return ReceiverBounds.Intersects(bounds);
        }
    }

    public static unsafe class ToonBlobShadowsCulling
    {
        private static readonly ProfilerMarker Marker = new("BlobShadows.ScheduleCulling");

        public static void ScheduleCulling(ref ToonBlobShadowsBatching.BatchData batch, in Bounds2D receiverBounds)
        {
            using (Marker.Auto())
            {
                var visibleIndices = new NativeList<int>(batch.Count, Allocator.TempJob);
                JobHandle jobHandle = new ToonBlobShadowsCullingJob
                    {
                        Data = batch.Group.DataPtr,
                        BaseIndex = batch.BaseIndex,
                        ReceiverBounds = receiverBounds,
                    }
                    .ScheduleAppend(visibleIndices, batch.Count);
                batch.CullingJobHandle = jobHandle;
                batch.VisibleIndices = visibleIndices;
            }
        }
    }
}