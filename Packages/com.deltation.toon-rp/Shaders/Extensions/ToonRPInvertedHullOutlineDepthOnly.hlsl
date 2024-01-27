#ifndef TOON_RP_INVERTED_HULL_OUTLINE_DEPTH_ONLY
#define TOON_RP_INVERTED_HULL_OUTLINE_DEPTH_ONLY

#include "ToonRPInvertedHullOutlineCommon.hlsl"
#include "ToonRPInvertedHullOutlineAppdata.hlsl"

struct v2f
{
    float4 positionCs : SV_POSITION;
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

    return OUT;
}

float4 PS() : SV_TARGET
{
    return 0;
}


#endif // TOON_RP_INVERTED_HULL_OUTLINE_DEPTH_ONLY