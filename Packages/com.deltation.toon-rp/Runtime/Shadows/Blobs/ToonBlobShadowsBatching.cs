using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Pool;

namespace DELTation.ToonRP.Shadows.Blobs
{
    public class ToonBlobShadowsBatching
    {
        private const int MaxBatchSize = 128;
        private static readonly ProfilerMarker Marker = new("BlobShadows.Batch");
        private static readonly ProfilerMarker FindBatchMarker = new("BlobShadows.FindBatch");
        private static readonly ProfilerMarker AddItemToBatchMarker = new("BlobShadows.AddItemToBatch");

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

        public void Batch(ToonBlobShadowsManager manager, NativeList<int> visibleIndices)
        {
            using ProfilerMarker.AutoScope scope = Marker.Auto();

            foreach (int index in visibleIndices)
            {
                ref readonly ToonBlobShadowsRendererData rendererData = ref manager.Renderers[index].GetRendererData();
                ref BatchData batchData = ref FindOrAllocateBatch(rendererData.ShadowType);

                using (AddItemToBatchMarker.Auto())
                {
                    batchData.Positions.Add(new Vector4(rendererData.Position.x, rendererData.Position.y,
                            rendererData.HalfSize, rendererData.HalfSize
                        )
                    );
                    batchData.Params.Add(rendererData.Params);
                    batchData.Count++;
                }
            }
        }

        public void FillGapsInBatches()
        {
            foreach (BatchSet batchSet in _batches)
            {
                for (int index = 0; index < batchSet.BatchCount; index++)
                {
                    ref readonly BatchData batchData = ref batchSet.Batches[index];

                    while (batchData.Positions.Count < MaxBatchSize)
                    {
                        batchData.Positions.Add(Vector4.zero);
                        batchData.Params.Add(Vector4.zero);
                    }
                }
            }
        }

        private ref BatchData FindOrAllocateBatch(ToonBlobShadowType shadowType)
        {
            using ProfilerMarker.AutoScope autoScope = FindBatchMarker.Auto();

            ref BatchSet batchSet = ref _batches[(int) shadowType];

            for (int i = 0; i < batchSet.BatchCount; i++)
            {
                ref BatchData batchData = ref batchSet.Batches[i];
                if (batchData.Count < MaxBatchSize)
                {
                    return ref batchData;
                }
            }

            int index = batchSet.BatchCount;
            batchSet.BatchCount++;

            if (index >= batchSet.Batches.Length)
            {
                ExpandArray(ref batchSet.Batches);
            }

            ref BatchData newBatchData = ref batchSet.Batches[index];
            newBatchData = BatchData.Create();
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

                for (int j = 0; j < batchSet.BatchCount; j++)
                {
                    ref BatchData batchData = ref batchSet.Batches[j];
                    batchData.Dispose();
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

        public struct BatchData : IDisposable
        {
            public readonly List<Vector4> Positions;
            public readonly List<Vector4> Params;
            public int Count;

            private BatchData(List<Vector4> positions, List<Vector4> @params)
            {
                Positions = positions;
                Params = @params;
                Count = 0;
            }

            public static BatchData Create() =>
                new(ListPool<Vector4>.Get(), ListPool<Vector4>.Get());

            public void Dispose()
            {
                if (Positions != null)
                {
                    ListPool<Vector4>.Release(Positions);
                }

                if (Params != null)
                {
                    ListPool<Vector4>.Release(Params);
                }
            }
        }
    }
}