using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;

namespace DELTation.ToonRP.Shadows.Blobs
{
    public unsafe class ToonBlobShadowsBatching
    {
        private const int MaxBatchSize = 256;
        private static readonly ProfilerMarker Marker = new("BlobShadows.Batch");
        private static readonly ProfilerMarker FindBatchMarker = new("BlobShadows.FindBatch");

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

        public void Batch(ToonBlobShadowsManager manager, ToonBlobShadowType shadowType, NativeList<int> visibleIndices)
        {
            using ProfilerMarker.AutoScope scope = Marker.Auto();

            ToonBlobShadowsManager.Group group = manager.GetGroup(shadowType);

            int left = 0;
            int right = visibleIndices.Length;

            while (left < right)
            {
                int count = right - left;
                if (count > MaxBatchSize)
                {
                    count = MaxBatchSize;
                }

                ref readonly BatchData batchData = ref CreateBatch(group, shadowType, left, count);

                // Copy a piece of visible indices array to the batch 
                fixed (float* batchIndicesPtr = batchData.Indices)
                {
                    UnsafeUtility.MemCpy(batchIndicesPtr, visibleIndices.GetUnsafePtr() + left, count * sizeof(int));
                }

                left += count;
            }
        }

        private ref readonly BatchData CreateBatch(ToonBlobShadowsManager.Group group, ToonBlobShadowType type,
            int baseIndex, int count)
        {
            using ProfilerMarker.AutoScope autoScope = FindBatchMarker.Auto();

            ref BatchSet batchSet = ref _batches[(int) type];

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
            for (int i = 0; i < _batches.Length; i++)
            {
                ref BatchSet batchSet = ref _batches[i];
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
            // Using floats instead of ints since Unity does not have cmd.SetGlobalIntArray
            public readonly float[] Indices;

            public ToonBlobShadowsManager.Group Group;
            public int BaseIndex;
            public int Count;

            private BatchData(float[] indices)
            {
                Indices = indices;
                Group = default;
                BaseIndex = default;
                Count = default;
            }

            public static BatchData Create() => new(new float[MaxBatchSize]);
        }
    }
}