using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using static DELTation.ToonRP.Shadows.Blobs.ToonBlobShadowsBatching;

namespace DELTation.ToonRP.Shadows.Blobs
{
    public sealed unsafe class ToonBlobShadowsGroup : IDisposable
    {
        public const int MinSize = MaxBatchSize;

        private const Allocator PackedDataAllocator = Allocator.Persistent;
        private const NativeArrayOptions DefaultArrayOptions = NativeArrayOptions.UninitializedMemory;

        private NativeArray<ToonBlobShadowPackedData> _packedData;
        private int _size;

        public ToonBlobShadowsGroup(int startSize = MinSize)
        {
            startSize = Mathf.Max(MinSize, startSize);
            _packedData =
                new NativeArray<ToonBlobShadowPackedData>(startSize, PackedDataAllocator, DefaultArrayOptions);
            PackedDataConstantBuffer = CreateConstantBuffer(startSize);
        }

        public int Size
        {
            get => _size;
            set
            {
                if (value > _packedData.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        $"Count cannot be greater than the array length ({_packedData.Length})"
                    );
                }

                _size = value;
            }
        }

        public GraphicsBuffer PackedDataConstantBuffer { get; private set; }

        internal ToonBlobShadowPackedData* PackedDataPtr => (ToonBlobShadowPackedData*) _packedData.GetUnsafePtr();

        public void Dispose()
        {
            _packedData.Dispose();
            _packedData = default;
            PackedDataConstantBuffer.Release();
        }

        public void ExpandData()
        {
            ToonBlobShadowsArrayUtils.ExpandArray(ref _packedData, PackedDataAllocator, DefaultArrayOptions);

            GraphicsBuffer newGpuData = CreateConstantBuffer(_packedData.Length * 2);
            PackedDataConstantBuffer.Release();
            PackedDataConstantBuffer = newGpuData;
        }

        public void PushDataToGPU()
        {
            PackedDataConstantBuffer.SetData(_packedData, 0, 0, Size);
        }

        private static GraphicsBuffer CreateConstantBuffer(int size)
        {
            // Align to the max batch size
            size = (size + MaxBatchSize - 1) / MaxBatchSize * MaxBatchSize;
            int stride = UnsafeUtility.SizeOf<float4>();
            return new GraphicsBuffer(GraphicsBuffer.Target.Constant,
                size * UnsafeUtility.SizeOf<ToonBlobShadowPackedData>() / stride
                , stride
            );
        }
    }
}