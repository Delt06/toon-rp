using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static DELTation.ToonRP.Shadows.Blobs.ToonPackingUtility;

namespace DELTation.ToonRP.Shadows.Blobs
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct ToonBlobShadowPackedParams
    {
        public const float DefaultOffsetMultiplier = 1.0f;
        public const float DefaultSaturation = 1.0f;

        public readonly ushort Param0;
        public readonly ushort Param1;
        public readonly ushort Param2;
        public readonly ushort Param3;

        public ToonBlobShadowPackedParams(ushort param0, ushort param1, ushort param2, ushort param3)
        {
            Param0 = param0;
            Param1 = param1;
            Param2 = param2;
            Param3 = param3;
        }

        public static ToonBlobShadowPackedParams PackCircle(float offsetMultiplier = DefaultOffsetMultiplier,
            float saturation = DefaultSaturation)
        {
            ushort sharedParams = PackSharedParams(offsetMultiplier, saturation);
            return new ToonBlobShadowPackedParams(0, sharedParams, 0, 0);
        }

        public static ToonBlobShadowPackedParams PackSquare(in ToonBlobShadowSquareParams shadow,
            float rotation = 0.0f,
            float offsetMultiplier = DefaultOffsetMultiplier, float saturation = DefaultSaturation)
        {
            ushort sharedParams = PackSharedParams(offsetMultiplier, saturation);
            return new ToonBlobShadowPackedParams(
                PackToShort(PackRotation(rotation, shadow.Rotation), 0),
                sharedParams,
                PackToShort(PackAsUNorm(shadow.Width * 0.5f), PackAsUNorm(shadow.Height * 0.5f)),
                PackToShort(PackAsUNorm(shadow.CornerRadius), 0)
            );
        }

        public static ToonBlobShadowPackedParams PackBaked(in ToonBlobShadowBakedParams shadow,
            float rotation = 0.0f,
            float offsetMultiplier = DefaultOffsetMultiplier, float saturation = DefaultSaturation)
        {
            ushort sharedParams = PackSharedParams(offsetMultiplier, saturation);
            return new ToonBlobShadowPackedParams(
                PackToShort(PackRotation(rotation, shadow.Rotation), 0),
                sharedParams,
                PackToShort((byte) shadow.TextureIndex, 0),
                PackToShort(0, 0)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort PackSharedParams(float offsetMultiplier, float saturation) =>
            PackToShort(PackAsSNorm(offsetMultiplier), PackAsUNorm(saturation));

        private static byte PackRotation(in float transformRotationY, float paramsRotation)
        {
            float rotation01 = (-transformRotationY + paramsRotation) / 360.0f;
            if (rotation01 < 0.0f)
            {
                rotation01 += 1.0f;
            }

            return PackAsUNorm(rotation01 % 1.0f);
        }
    }
}