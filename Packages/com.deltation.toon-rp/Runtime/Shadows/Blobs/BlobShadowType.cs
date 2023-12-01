using System;

namespace DELTation.ToonRP.Shadows.Blobs
{
    public enum BlobShadowType : byte
    {
        Circle = 0,
        Square,
        Baked,
    }

    public static class BlobShadowTypes
    {
        public static readonly int Count = Enum.GetValues(typeof(BlobShadowType)).Length;
    }
}