using System;

namespace DELTation.ToonRP.Shadows
{
    public enum BlobShadowType : byte
    {
        Circle = 0,
        Square,
    }

    public static class BlobShadowTypes
    {
        public static readonly int Count = Enum.GetValues(typeof(BlobShadowType)).Length;
    }
}