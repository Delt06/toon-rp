﻿#ifndef TOON_RP_INVERTED_HULL_OUTLINE_DEPTH_NORMALS
#define TOON_RP_INVERTED_HULL_OUTLINE_DEPTH_NORMALS

#include "ToonRPInvertedHullOutlineCommon.hlsl"
#include "ToonRPInvertedHullOutlineAppdata.hlsl"

#include "../../ShaderLibrary/DepthNormals.hlsl"

struct v2f
{
    float4 positionCs : SV_POSITION;
    float3 normalWs : NORMAL_WS;
};

v2f VS(const appdata IN)
{
    v2f OUT;

    // ReSharper disable once CppLocalVariableMayBeConst
    float3 positionWs = TransformObjectToWorld(IN.vertex);
    const float3 normalWs = TransformObjectToWorldNormal(IN.normal);

    const float thickness = ComputeThickness(TOON_RP_OUTLINES_UV(IN), positionWs, normalWs);
    const float4 positionCs = ApplyThicknessAndTransformToHClip(positionWs, normalWs, thickness);
    OUT.positionCs = positionCs;
    OUT.normalWs = normalWs;

    return OUT;
}

float4 PS(const v2f IN) : SV_TARGET
{
    float3 normalWs = IN.normalWs;
    normalWs = normalize(normalWs);
    return float4(PackNormal(normalWs), 0);
}

#endif // TOON_RP_INVERTED_HULL_OUTLINE_DEPTH_NORMALS