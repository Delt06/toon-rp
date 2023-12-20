using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Unity.Mathematics.math;

namespace DELTation.ToonRP.Shadows.Blobs
{
    public static class ToonPackingUtility
    {
        /// <summary>Returns the bit pattern of a float as a uint.</summary>
        /// <param name="x">The float bits to copy.</param>
        /// <returns>The uint with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint FastAsUint(float x)
        {
            UIntFloatUnion u;
            u.uintValue = 0;
            u.floatValue = x;
            return u.uintValue;
        }

        /// <summary>Returns the bit pattern of a uint as a float.</summary>
        /// <param name="x">The uint bits to copy.</param>
        /// <returns>The float with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float FastAsFloat(uint x)
        {
            UIntFloatUnion u;
            u.floatValue = 0;
            u.uintValue = x;

            return u.floatValue;
        }

        /// <summary>
        ///     A version of FloatToHalf with some assumptions (modified math.f32tof16()):
        ///     1. No NaNs
        ///     2. No infinities
        ///     3. No overflow
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort FloatToHalfFast(float x)
        {
            const uint msk = 0x7FFFF000u;

            uint ux = FastAsUint(x);
            uint uux = ux & msk;
            uint h = (FastAsUint(FastAsFloat(uux) * 1.92592994e-34f) + 0x1000) >> 13;
            return (ushort) (h | ((ux & ~msk) >> 16));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte PackAsUNorm(float value) => PackAsUNormUnclamped(saturate(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte PackAsUNormUnclamped(float value) => (byte) (value * 255.0f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte PackAsSNorm(float value)
        {
            float value01 = saturate(value * 0.5f + 0.5f);
            return PackAsUNorm(value01);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort PackToShort(byte value1, byte value2) => (ushort) ((value2 << 8) | value1);

        [StructLayout(LayoutKind.Explicit)]
        private struct UIntFloatUnion
        {
            [FieldOffset(0)]
            public uint uintValue;
            [FieldOffset(0)]
            public float floatValue;
        }
    }
}