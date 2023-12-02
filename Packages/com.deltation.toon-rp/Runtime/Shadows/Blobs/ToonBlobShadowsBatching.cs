using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Pool;
using static Unity.Mathematics.math;

namespace DELTation.ToonRP.Shadows.Blobs
{
    public class ToonBlobShadowsBatching
    {
        private const int MaxBatchSize = 128;
        private static readonly ProfilerMarker Marker = new("BlobShadows.Batch");

        private BatchData[] _batches = new BatchData[16];

        public BatchData[] Batches => _batches;

        public int BatchCount { get; private set; }

        public void Batch(List<ToonBlobShadowsCulling.RendererData> renderers)
        {
            using ProfilerMarker.AutoScope scope = Marker.Auto();

            foreach (ToonBlobShadowsCulling.RendererData rendererData in renderers)
            {
                var key = new BatchKey(rendererData.ShadowType, rendererData.BakedShadowTexture);
                ref BatchData batchData = ref FindOrAllocateBatch(key);

                batchData.Positions.Add(float4(rendererData.Position, rendererData.HalfSize, rendererData.HalfSize));
                batchData.Params.Add(rendererData.Params);
            }
        }

        private ref BatchData FindOrAllocateBatch(in BatchKey key)
        {
            for (int i = 0; i < BatchCount; i++)
            {
                ref BatchData batchData = ref Batches[i];
                if (batchData.Key.Equals(key) && batchData.Positions.Count < MaxBatchSize)
                {
                    return ref batchData;
                }
            }

            if (BatchCount >= _batches.Length)
            {
                ResizeBatches();
            }

            ref BatchData newBatchData = ref Batches[BatchCount++];
            newBatchData = BatchData.Get(key);
            return ref newBatchData;
        }

        private void ResizeBatches()
        {
            int newSize = _batches.Length * 2;
            Array.Resize(ref _batches, newSize);
        }

        public void Clear()
        {
            for (int i = 0; i < BatchCount; i++)
            {
                ref BatchData batchData = ref Batches[i];
                batchData.Dispose();
                batchData = default;
            }

            BatchCount = 0;
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
            public readonly BlobShadowType ShadowType;
            [CanBeNull]
            public readonly Texture2D BakedTexture;

            public BatchKey(BlobShadowType shadowType, [CanBeNull] Texture2D bakedTexture)
            {
                ShadowType = shadowType;
                BakedTexture = bakedTexture;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(BatchKey other) =>
                ShadowType == other.ShadowType && Equals(BakedTexture, other.BakedTexture);

            public override bool Equals(object obj) => obj is BatchKey other && Equals(other);

            public override int GetHashCode() => HashCode.Combine((int) ShadowType, BakedTexture);
        }
    }
}