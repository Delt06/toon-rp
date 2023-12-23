using System.Diagnostics.CodeAnalysis;
using Unity.Mathematics;

namespace DELTation.ToonRP.Shadows.Blobs
{
    [SuppressMessage("ReSharper", "NotAccessedField.Global")]
    public struct ToonBlobShadowPackedData
    {
        public half4 PositionSize;
        public ToonBlobShadowPackedParams Params;
    }
}