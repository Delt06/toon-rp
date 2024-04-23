using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Lighting
{
    [GenerateHLSL]
    public struct TiledLight
    {
        public Vector4 Color; // rgb = color
        public Vector4 BoundingSphere_CenterVs_Radius; // xyz = center VS, w = radius
        public Vector4 PositionWs_Attenuation; // xyz = position WS, w = 1/range^2
        public Vector4 ConeBoundingSphere_CenterVs_Radius; // xyz = center VS, w = radius
    }
}