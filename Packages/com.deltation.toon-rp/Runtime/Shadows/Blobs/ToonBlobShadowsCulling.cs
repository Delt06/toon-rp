using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;

namespace DELTation.ToonRP.Shadows.Blobs
{
    [BurstCompile]
    public unsafe struct ToonBlobShadowsCullingJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<CullingGroup> CullingGroups;
        [WriteOnly] [NativeDisableParallelForRestriction]
        public NativeArray<int> SharedIndices;
        public NativeArray<int> SharedCounters;

        public Bounds2D ReceiverBounds;

        public struct CullingGroup
        {
            [NativeDisableUnsafePtrRestriction]
            public ToonBlobShadowPackedData* Data;
            public int BaseIndex;
            public int Count;
        }

        public void Execute(int index)
        {
            CullingGroup cullingGroup = CullingGroups[index];
            int indicesBase = index * ToonBlobShadowsBatching.MaxBatchSize;

            for (int localRendererIndex = 0; localRendererIndex < cullingGroup.Count; localRendererIndex++)
            {
                ref ToonBlobShadowPackedData packedData =
                    ref cullingGroup.Data[cullingGroup.BaseIndex + localRendererIndex];
                var positionSize = (float4) packedData.PositionSize;
                var bounds = Bounds2D.FromCenterExtents(positionSize.xy, positionSize.zw);
                if (ReceiverBounds.Intersects(bounds))
                {
                    int previousCounterValue = SharedCounters[index]++;
                    SharedIndices[indicesBase + previousCounterValue] = localRendererIndex;
                }
            }
        }
    }

    public struct ToonBlobShadowsCullingHandle
    {
        public JobHandle JobHandle;
        public NativeArray<ToonBlobShadowsCullingJob.CullingGroup> CullingGroups;
        public NativeArray<int> SharedIndices;
        public NativeArray<int> SharedCounters;

        public ToonBlobShadowsCullingHandle(JobHandle jobHandle,
            NativeArray<ToonBlobShadowsCullingJob.CullingGroup> cullingGroups,
            NativeArray<int> sharedIndices, NativeArray<int> sharedCounters)
        {
            JobHandle = jobHandle;
            CullingGroups = cullingGroups;
            SharedIndices = sharedIndices;
            SharedCounters = sharedCounters;
        }

        public bool IsEmpty => CullingGroups.Length == 0;

        public void Complete() => JobHandle.Complete();

        public void Dispose()
        {
            JobHandle = default;
            CullingGroups.Dispose();
            SharedIndices.Dispose();
            SharedCounters.Dispose();
        }
    }

    public static class ToonBlobShadowsCulling
    {
        private static readonly ProfilerMarker Marker = new("BlobShadows.ScheduleCulling");

        public static unsafe ToonBlobShadowsCullingHandle ScheduleCulling(ToonBlobShadowsBatching batching,
            in Bounds2D receiverBounds)
        {
            using (Marker.Auto())
            {
                int totalCullingGroups = 0;

                for (int shadowTypeIndex = 0; shadowTypeIndex < ToonBlobShadowTypes.Count; shadowTypeIndex++)
                {
                    ToonBlobShadowsBatching.BatchSet batchSet =
                        batching.GetBatches((ToonBlobShadowType) shadowTypeIndex);
                    totalCullingGroups += batchSet.BatchCount;
                }

                if (totalCullingGroups == 0)
                {
                    return default;
                }

                var cullingGroups =
                    new NativeArray<ToonBlobShadowsCullingJob.CullingGroup>(totalCullingGroups, Allocator.TempJob);
                var sharedIndices = new NativeArray<int>(totalCullingGroups * ToonBlobShadowsBatching.MaxBatchSize,
                    Allocator.TempJob
                );
                var sharedCounters = new NativeArray<int>(totalCullingGroups, Allocator.TempJob);
                int cullingGroupIndex = 0;

                for (int shadowTypeIndex = 0; shadowTypeIndex < ToonBlobShadowTypes.Count; shadowTypeIndex++)
                {
                    ToonBlobShadowsBatching.BatchSet batchSet =
                        batching.GetBatches((ToonBlobShadowType) shadowTypeIndex);
                    for (int batchIndex = 0; batchIndex < batchSet.BatchCount; batchIndex++)
                    {
                        ref ToonBlobShadowsBatching.BatchData batch = ref batchSet.Batches[batchIndex];

                        batch.CullingGroupIndex = cullingGroupIndex;

                        cullingGroups[cullingGroupIndex] = new ToonBlobShadowsCullingJob.CullingGroup
                        {
                            Data = batch.Group.PackedDataPtr,
                            BaseIndex = batch.BaseIndex,
                            Count = batch.Count,
                        };

                        cullingGroupIndex++;
                    }
                }

                JobHandle jobHandle = new ToonBlobShadowsCullingJob
                {
                    CullingGroups = cullingGroups,
                    SharedIndices = sharedIndices,
                    SharedCounters = sharedCounters,
                    ReceiverBounds = receiverBounds,
                }.Schedule(cullingGroups.Length, 1);
                return new ToonBlobShadowsCullingHandle(jobHandle, cullingGroups, sharedIndices, sharedCounters);
            }
        }
    }
}