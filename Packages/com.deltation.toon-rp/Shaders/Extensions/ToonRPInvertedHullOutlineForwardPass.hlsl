﻿#ifndef TOON_RP_INVERTED_HULL_OUTLINE_FORWARD_PASS
#define TOON_RP_INVERTED_HULL_OUTLINE_FORWARD_PASS

#include "ToonRPInvertedHullOutlineCommon.hlsl"

struct v2f
{
    float4 positionCs : SV_POSITION;
    TOON_RP_FOG_FACTOR_INTERPOLANT
};

v2f VS(const appdata IN)
{
    v2f OUT;

    const float3 positionWs = TransformObjectToWorld(IN.vertex);
    const float3 normalWs = TransformObjectToWorldNormal(IN.normal);

    const float thickness = ComputeThickness(IN, positionWs, normalWs);
    const float4 positionCs = ApplyThicknessAndTransformToHClip(positionWs, normalWs, thickness);
    OUT.positionCs = positionCs;

    TOON_RP_FOG_FACTOR_TRANSFER(OUT, positionCs);

    return OUT;
}

float4 PS(const v2f IN) : SV_TARGET
{
    float3 outputColor = _Color;
    TOON_RP_FOG_MIX(IN, outputColor);
    return float4(outputColor, 1);
}


#endif // TOON_RP_INVERTED_HULL_OUTLINE_FORWARD_PASS