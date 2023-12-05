using System;
using Unity.Collections;
using Unity.Profiling;

namespace DELTation.ToonRP.Shadows.Blobs
{
    public class ToonBlobShadowsBatching
    {
        private const int MaxBatchSize = 128;
        private static readonly ProfilerMarker Marker = new("BlobShadows.Batch");
        private static readonly ProfilerMarker FindBatchMarker = new("BlobShadows.FindBatch");

        private readonly BatchSet[] _batches = new BatchSet[ToonBlobShadowTypes.Count];

        public ToonBlobShadowsBatching()
        {
            for (int i = 0; i < ToonBlobShadowTypes.Count; i++)
            {
                ref BatchSet batchSet = ref _batches[i];
                batchSet.ShadowType = (ToonBlobShadowType) i;
                batchSet.Batches ??= new BatchData[16];
            }
        }

        public BatchSet GetBatches(ToonBlobShadowType type) => _batches[(int) type];

        public void Batch(ToonBlobShadowsManager manager, ToonBlobShadowType shadowType, NativeList<int> visibleIndices)
        {
            using ProfilerMarker.AutoScope scope = Marker.Auto();

            ToonBlobShadowsManager.Group group = manager.GetGroup(shadowType);

            int left = 0;
            int right = group.Renderers.Count;

            while (left < right)
            {
                int count = right - left;
                if (count > MaxBatchSize)
                {
                    count = MaxBatchSize;
                }

                CreateBatch(group, shadowType, left, count);
                left += count;
            }
        }

        private void CreateBatch(ToonBlobShadowsManager.Group group, ToonBlobShadowType type, int baseIndex, int count)
        {
            using ProfilerMarker.AutoScope autoScope = FindBatchMarker.Auto();

            ref BatchSet batchSet = ref _batches[(int) type];

            int index = batchSet.BatchCount;
            batchSet.BatchCount++;

            if (index >= batchSet.Batches.Length)
            {
                ExpandArray(ref batchSet.Batches);
            }

            ref BatchData newBatchData = ref batchSet.Batches[index];
            newBatchData = new BatchData(group, baseIndex, count);
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

                for (int j = 0; j < batchSet.BatchCount; j++)
                {
                    ref BatchData batchData = ref batchSet.Batches[j];
                    batchData = default;
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

        public readonly struct BatchData
        {
            public readonly ToonBlobShadowsManager.Group Group;
            public readonly int BaseIndex;
            public readonly int Count;

            public BatchData(ToonBlobShadowsManager.Group group, int baseIndex, int count)
            {
                Group = group;
                BaseIndex = baseIndex;
                Count = count;
            }
        }
    }
}