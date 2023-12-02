using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Pool;

namespace DELTation.ToonRP.Shadows.Blobs
{
    public class ToonBlobShadowsBatching
    {
        private const int MaxBatchSize = 128;
        private static readonly ProfilerMarker Marker = new("BlobShadows.Batch");

        private readonly BatchSet[] _batches = new BatchSet[ToonBlobShadowTypes.Count];

        public ToonBlobShadowsBatching()
        {
            for (int i = 0; i < ToonBlobShadowTypes.Count; i++)
            {
                _batches[i].ShadowType = (ToonBlobShadowType) i;
            }
        }

        public BatchSet GetBatches(ToonBlobShadowType type) => _batches[(int) type];

        public void Batch(ToonBlobShadowsManager manager, List<int> visibleIndices)
        {
            using ProfilerMarker.AutoScope scope = Marker.Auto();

            foreach (int index in visibleIndices)
            {
                ref readonly ToonBlobShadowsRendererData rendererData = ref manager.Renderers[index].GetRendererData();
                var key = new BatchKey(rendererData.BakedShadowTexture);
                ref BatchData batchData = ref FindOrAllocateBatch(rendererData.ShadowType, key);

                batchData.Positions.Add(new Vector4(rendererData.Position.x, rendererData.Position.y,
                        rendererData.HalfSize, rendererData.HalfSize
                    )
                );
                batchData.Params.Add(rendererData.Params);
            }
        }

        private ref BatchData FindOrAllocateBatch(ToonBlobShadowType shadowType, in BatchKey key)
        {
            ref BatchSet batchSet = ref _batches[(int) shadowType];

            batchSet.Batches ??= new BatchData[16];

            for (int i = 0; i < batchSet.BatchCount; i++)
            {
                ref BatchData batchData = ref batchSet.Batches[i];
                if (batchData.Key.Equals(key) && batchData.Positions.Count < MaxBatchSize)
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
            newBatchData = BatchData.Get(key);
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

        public readonly struct BatchData : IDisposable
        {
            public readonly BatchKey Key;
            public readonly List<Vector4> Positions;
            public readonly List<Vector4> Params;

            private BatchData(BatchKey key, List<Vector4> positions, List<Vector4> @params)
            {
                Positions = positions;
                Params = @params;
                Key = key;
            }

            public static BatchData Get(BatchKey key) =>
                new(key, ListPool<Vector4>.Get(), ListPool<Vector4>.Get());

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

        public readonly struct BatchKey : IEquatable<BatchKey>
        {
            [CanBeNull]
            public readonly Texture2D BakedTexture;

            public BatchKey([CanBeNull] Texture2D bakedTexture) => BakedTexture = bakedTexture;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(BatchKey other) => Equals(BakedTexture, other.BakedTexture);

            public override bool Equals(object obj) => obj is BatchKey other && Equals(other);

            public override int GetHashCode() => BakedTexture ? BakedTexture.GetHashCode() : 0;
        }
    }
}