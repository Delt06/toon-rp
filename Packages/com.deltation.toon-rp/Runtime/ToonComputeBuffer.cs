using System;
using UnityEngine;

namespace DELTation.ToonRP
{
    public class ToonComputeBuffer : IDisposable
    {
        private readonly ComputeBufferType _bufferType;
        private readonly int _stride;
        private ComputeBuffer _buffer;
        private int _count;

        public ToonComputeBuffer(ComputeBufferType bufferType, int stride, int countGrowStep = 1)
        {
            _bufferType = bufferType;
            _stride = stride;
            CountGrowStep = countGrowStep;
        }

        private int CountGrowStep { get; }

        private int CountShrinkStep => CountGrowStep * 2;

        public ComputeBuffer Buffer
        {
            get
            {
                if (_buffer == null)
                {
                    Update(1);
                }

                return _buffer;
            }
        }

        public void Dispose()
        {
            _buffer?.Dispose();
        }

        public void Update(int desiredCount)
        {
            desiredCount = Mathf.Max(desiredCount, 1);

            int alignment = desiredCount < _count ? CountShrinkStep : CountGrowStep;
            int count = AlignUp(desiredCount, alignment);
            RecreateWithCount(count);
        }

        private static int AlignUp(int value, int alignment) => (value + alignment - 1) & ~(alignment - 1);

        private void RecreateWithCount(int count)
        {
            _buffer?.Release();
            _buffer = new ComputeBuffer(count, _stride, _bufferType);
            _count = count;
        }
    }
}