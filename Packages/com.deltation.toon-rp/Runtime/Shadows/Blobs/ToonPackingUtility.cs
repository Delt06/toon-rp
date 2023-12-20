using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace DELTation.ToonRP.Shadows.Blobs
{
    public static class ToonPackingUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort FloatToHalf(float value) => (ushort) math.f32tof16(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte PackAsUNorm(float value) => (byte) (math.saturate(value) * 255);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte PackAsSNorm(float value)
        {
            float value01 = math.saturate(value * 0.5f + 0.5f);
            return PackAsUNorm(value01);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort PackToShort(byte value1, byte value2) => (ushort) ((value2 << 8) | value1);
    }
}