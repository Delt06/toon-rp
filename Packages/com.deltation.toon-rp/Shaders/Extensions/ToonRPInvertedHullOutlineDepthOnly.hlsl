#ifndef TOON_RP_INVERTED_HULL_OUTLINE_DEPTH_ONLY
#define TOON_RP_INVERTED_HULL_OUTLINE_DEPTH_ONLY

#include "ToonRPInvertedHullOutlineCommon.hlsl"

struct v2f
{
    float4 positionCs : SV_POSITION;
};

v2f VS(const appdata IN)
{
    v2f OUT;

    const float3 positionWs = TransformObjectToWorld(IN.vertex);
    const float3 normalWs = TransformObjectToWorldNormal(IN.normal);

    const float thickness = ComputeThickness(IN, positionWs, normalWs);
    const float4 positionCs = ApplyThicknessAndTransformToHClip(positionWs, normalWs, thickness);
    OUT.positionCs = positionCs;

    return OUT;
}

float4 PS() : SV_TARGET
{
    return 0;
}


#endif // TOON_RP_INVERTED_HULL_OUTLINE_DEPTH_ONLY