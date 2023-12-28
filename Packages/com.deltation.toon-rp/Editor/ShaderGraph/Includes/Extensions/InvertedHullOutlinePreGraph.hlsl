#ifndef TOON_RP_INVERTED_HULL_OUTLINE_PRE_GRAPH
#define TOON_RP_INVERTED_HULL_OUTLINE_PRE_GRAPH

#include "Packages/com.deltation.toon-rp/Shaders/Extensions/ToonRPInvertedHullOutlineCommon.hlsl"

float4 InvertedHullOutline_TransformWorldToHClip(const float3 positionWs, const float3 normalWs, const float2 uv, const float thicknessMultiplier)
{
    const float thickness = ComputeThickness(uv, positionWs, normalWs) * thicknessMultiplier;
    const float4 positionCs = ApplyThicknessAndTransformToHClip(positionWs, normalWs, thickness);
    return positionCs;
}

#define TRANSFORM_WORLD_TO_HCLIP(positionWs, normalWs, appdata, vertexDescription) (InvertedHullOutline_TransformWorldToHClip(positionWs, normalWs, appdata.uv0.xy, vertexDescription.OutlineThickness))

#ifdef _NORMAL_SEMANTIC_TANGENT
#define SHADER_GRAPH_NORMAL_SOURCE_TANGENT
#endif // _NORMAL_SEMANTIC_TANGENT

#ifdef _NORMAL_SEMANTIC_UV2
#define SHADER_GRAPH_NORMAL_SOURCE_UV2
#endif // _NORMAL_SEMANTIC_UV2

#endif // TOON_RP_INVERTED_HULL_OUTLINE_PRE_GRAPH