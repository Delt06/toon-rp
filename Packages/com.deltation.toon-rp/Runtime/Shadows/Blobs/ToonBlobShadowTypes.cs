using System;
using System.Linq;

namespace DELTation.ToonRP.Shadows.Blobs
{
    public static class ToonBlobShadowTypes
    {
        public static readonly int Count;
        public static readonly string[] Names;

        static ToonBlobShadowTypes()
        {
            var values = (ToonBlobShadowType[]) Enum.GetValues(typeof(ToonBlobShadowType));
            Count = values.Length;
            Names = values.Select(t => t.ToString()).ToArray();
        }
    }
}