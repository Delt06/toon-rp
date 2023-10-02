using UnityEngine;

namespace DELTation.ToonRP.Lighting
{
    public struct TiledLight
    {
        public Vector4 Color; // rgb = color
        public Vector4 PositionVsRange; // xyz = position VS, w = range
        public Vector4 PositionWsAttenuation; // xyz = position, w = 1/range^2
    }
}