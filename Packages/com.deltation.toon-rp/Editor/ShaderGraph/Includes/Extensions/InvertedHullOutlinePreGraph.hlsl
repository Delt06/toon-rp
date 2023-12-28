#ifndef TOON_RP_INVERTED_HULL_OUTLINE_PRE_GRAPH
#define TOON_RP_INVERTED_HULL_OUTLINE_PRE_GRAPH

#include "Packages/com.deltation.toon-rp/Shaders/Extensions/ToonRPInvertedHullOutlineCommon.hlsl"

float4 InvertedHullOutline_TransformWorldToHClip(inout float3 positionWs, const float3 normalWs, const float2 uv)
{
    const float thickness = ComputeThickness(uv, positionWs, normalWs);
    const float4 positionCs = ApplyThicknessAndTransformToHClip(positionWs, normalWs, thickness);
    return positionCs;
}

#define TRANSFORM_WORLD_TO_HCLIP(positionWs, normalWs, appdata) (InvertedHullOutline_TransformWorldToHClip(positionWs, normalWs, appdata.uv0.xy))

#endif // TOON_RP_INVERTED_HULL_OUTLINE_PRE_GRAPH