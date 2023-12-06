using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;

namespace DELTation.ToonRP.Shadows.Blobs
{
    public class ToonBlobShadowsBatching
    {
        // Mirrored in ToonRPBlobShadowPass.hlsl (BATCH_SIZE)
        public const int MaxBatchSize = 256;
        private static readonly ProfilerMarker BatchMarker = new("BlobShadows.Batch");
        private static readonly ProfilerMarker FindBatchMarker = new("BlobShadows.FindBatch");
        private static readonly ProfilerMarker UpdateRendererDataMarker = new("BlobShadows.UpdateRendererData");

        private readonly BatchSet[] _batches = new BatchSet[ToonBlobShadowTypes.Count];

        public ToonBlobShadowsBatching()
        {
            for (int i = 0; i < ToonBlobShadowTypes.Count; i++)
            {
                ref BatchSet batchSet = ref _batches[i];
                batchSet.ShadowType = (ToonBlobShadowType) i;
                batchSet.Batches = new[]
                {
                    BatchData.Create(),
                };
            }
        }

        public BatchSet GetBatches(ToonBlobShadowType type) => _batches[(int) type];

        public void Batch(ToonBlobShadowsManager.Group group, in Bounds2D receiverBounds)
        {
            if (group.Renderers.Count == 0)
            {
                return;
            }

            using (UpdateRendererDataMarker.Auto())
            {
                group.UpdateRendererData();
            }

            using (BatchMarker.Auto())
            {
                int left = 0;
                int right = group.Renderers.Count;

                while (left < right)
                {
                    int count = right - left;
                    if (count > MaxBatchSize)
                    {
                        count = MaxBatchSize;
                    }

                    ref BatchData batchData = ref CreateBatch(group, left, count);
                    ToonBlobShadowsCulling.ScheduleCulling(ref batchData, receiverBounds);
                    left += count;
                }
            }
        }

        private ref BatchData CreateBatch(ToonBlobShadowsManager.Group group, int baseIndex, int count)
        {
            using ProfilerMarker.AutoScope autoScope = FindBatchMarker.Auto();

            ref BatchSet batchSet = ref _batches[(int) group.ShadowType];

            int index = batchSet.BatchCount;
            batchSet.BatchCount++;

            if (index >= batchSet.Batches.Length)
            {
                ExpandArray(ref batchSet.Batches);

                for (int i = index; i < batchSet.Batches.Length; i++)
                {
                    batchSet.Batches[i] = BatchData.Create();
                }
            }

            ref BatchData newBatchData = ref batchSet.Batches[index];
            newBatchData.Group = group;
            newBatchData.BaseIndex = baseIndex;
            newBatchData.Count = count;
            return ref newBatchData;
        }

        private static void ExpandArray<T>(ref T[] array)
        {
            int newSize = array.Length * 2;
            Array.Resize(ref array, newSize);
        }

        public void Clear()
        {
            for (int setIndex = 0; setIndex < _batches.Length; setIndex++)
            {
                ref BatchSet batchSet = ref _batches[setIndex];

                for (int batchIndex = 0; batchIndex < batchSet.BatchCount; batchIndex++)
                {
                    ref BatchData batchData = ref batchSet.Batches[batchIndex];
                    batchData.CullingJobHandle = default;
                    batchData.VisibleIndices.Dispose();
                }

                batchSet.BatchCount = 0;
            }
        }

        public struct BatchSet
        {
            public ToonBlobShadowType ShadowType;
            public int BatchCount;
            public BatchData[] Batches;
        }

        public struct BatchData
        {
            public ToonBlobShadowsManager.Group Group;
            public int BaseIndex;
            public int Count;

            public JobHandle CullingJobHandle;
            public NativeList<int> VisibleIndices;

            public static BatchData Create() => new();
        }
    }
}