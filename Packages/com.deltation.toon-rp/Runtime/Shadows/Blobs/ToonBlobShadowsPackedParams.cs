using System.Runtime.InteropServices;

namespace DELTation.ToonRP.Shadows.Blobs
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct ToonBlobShadowsPackedParams
    {
        public readonly ushort Param0;
        public readonly ushort Param1;
        public readonly ushort Param2;
        public readonly ushort Param3;

        public ToonBlobShadowsPackedParams(ushort param0, ushort param1, ushort param2, ushort param3)
        {
            Param0 = param0;
            Param1 = param1;
            Param2 = param2;
            Param3 = param3;
        }
    }
}