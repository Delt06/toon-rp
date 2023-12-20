using System.Runtime.CompilerServices;
using static Unity.Mathematics.math;

namespace DELTation.ToonRP.Shadows.Blobs
{
    public static class ToonPackingUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort FloatToHalf(float value) => (ushort) f32tof16(value);


        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <summary>
        ///     A version of FloatToHalf with some assumptions (modified math.f32tof16()):
        ///     1. No NaNs
        ///     2. No infinities
        ///     3. No overflow
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static ushort FloatToHalfFast(float x)
        {
            const uint msk = 0x7FFFF000u;

            uint ux = asuint(x);
            uint uux = ux & msk;
            uint h = (asuint(min(asfloat(uux) * 1.92592994e-34f, 260042752.0f)) + 0x1000) >>
                     13; // Clamp to signed infinity if overflowed
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
    }
}