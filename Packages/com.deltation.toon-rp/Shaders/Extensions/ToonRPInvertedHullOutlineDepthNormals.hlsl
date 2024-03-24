#ifndef TOON_RP_INVERTED_HULL_OUTLINE_DEPTH_NORMALS
#define TOON_RP_INVERTED_HULL_OUTLINE_DEPTH_NORMALS

#include "ToonRPInvertedHullOutlineCommon.hlsl"
#include "ToonRPInvertedHullOutlineAppdata.hlsl"

#include "../../ShaderLibrary/DepthNormals.hlsl"

struct v2f
{
    float4 positionCs : SV_POSITION;
    float3 normalWs : NORMAL_WS;
    UNITY_VERTEX_OUTPUT_STEREO
};

v2f VS(const appdata IN)
{
    v2f OUT;

    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

    // ReSharper disable once CppLocalVariableMayBeConst
    float3 positionWs = TransformObjectToWorld(IN.vertex);
    const float3 normalWs = TransformObjectToWorldNormal(IN.normal);

    const float thickness = ComputeThickness(TOON_RP_OUTLINES_UV(IN), positionWs, normalWs);
    const float4 positionCs = ApplyThicknessAndTransformToHClip(positionWs, normalWs, thickness);
    OUT.positionCs = positionCs;
    OUT.normalWs = normalWs;

    return OUT;
}

float2 PS(const v2f IN) : SV_TARGET
{
    float3 normalWs = IN.normalWs;
    normalWs = normalize(normalWs);
    return PackNormal(normalWs);
}

#endif // TOON_RP_INVERTED_HULL_OUTLINE_DEPTH_NORMALS