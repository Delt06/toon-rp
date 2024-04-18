#ifndef TOON_RP_INVERTED_HULL_OUTLINE_MOTION_VECTORS
#define TOON_RP_INVERTED_HULL_OUTLINE_MOTION_VECTORS

#define EXTRA_APP_DATA float3 positionOld : TEXCOORD4;

#include "ToonRPInvertedHullOutlineCommon.hlsl"
#include "ToonRPInvertedHullOutlineAppdata.hlsl"

#include "../../ShaderLibrary/MotionVectors.hlsl"

struct v2f
{
    float4 positionCs : SV_POSITION;
    float4 positionCsNoJitter : POSITION_CS_NO_JITTER;
    float4 previousPositionCsNoJitter : PREVIOUS_POSITION_CS_NO_JITTER;
    UNITY_VERTEX_OUTPUT_STEREO
};

v2f VS(const appdata IN)
{
    v2f OUT;

    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

    // ReSharper disable once CppLocalVariableMayBeConst
    const float3 positionWs = TransformObjectToWorld(IN.vertex);
    const float3 normalWs = TransformObjectToWorldNormal(IN.normal);

    {
        const float thickness = ComputeThickness(TOON_RP_OUTLINES_UV(IN), positionWs, normalWs);
        OUT.positionCs = ApplyThicknessAndTransformToHClip(positionWs, normalWs, thickness);

        UNITY_BRANCH
        if (RenderZeroMotionVectors())
        {
            OUT.positionCsNoJitter = 0;
        }
        else
        {
            OUT.positionCsNoJitter = ApplyThicknessAndTransformToHClip(_NonJitteredViewProjMatrix, positionWs, normalWs, thickness);
        }
    }

    {
        const float3 previousPositionOs = UseLastFramePositions() ? IN.positionOld.xyz : IN.vertex;
        const float3 previousPositionWs = mul(UNITY_PREV_MATRIX_M, float4(previousPositionOs, 1)).xyz;
        const float thickness = ComputeThickness(TOON_RP_OUTLINES_UV(IN), previousPositionWs, normalWs);

        UNITY_BRANCH
        if (RenderZeroMotionVectors())
        {
            OUT.previousPositionCsNoJitter = 0;
        }
        else
        {
            OUT.previousPositionCsNoJitter = ApplyThicknessAndTransformToHClip(_PrevViewProjMatrix, previousPositionWs, normalWs, thickness);
        }
    }

    ApplyMotionVectorZBias(OUT.positionCs);

    return OUT;
}

float4 PS(const v2f IN) : SV_TARGET
{
    UNITY_BRANCH
    if (RenderZeroMotionVectors())
    {
        return 0;
    }
    
    return float4(CalcNdcMotionVectorFromCsPositions(IN.positionCsNoJitter, IN.previousPositionCsNoJitter), 0, 0);
}

#endif // TOON_RP_INVERTED_HULL_OUTLINE_MOTION_VECTORS