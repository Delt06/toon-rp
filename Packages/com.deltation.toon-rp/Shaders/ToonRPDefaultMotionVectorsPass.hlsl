#ifndef TOON_RP_DEFAULT_DEPTH_ONLY_PASS
#define TOON_RP_DEFAULT_DEPTH_ONLY_PASS

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/MotionVectors.hlsl"
#include "../ShaderLibrary/Textures.hlsl"

#if defined(_ALPHATEST_ON)
#define REQUIRE_UV_INTERPOLANT
#endif // _ALPHATEST_ON

struct appdata
{
    float3 vertex : POSITION;

    #ifdef REQUIRE_UV_INTERPOLANT
    float2 uv : TEXCOORD;
    #endif // REQUIRE_UV_INTERPOLANT

    float3 positionOld : TEXCOORD4;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    #ifdef REQUIRE_UV_INTERPOLANT
    float2 uv : TEXCOORD;
    #endif // REQUIRE_UV_INTERPOLANT

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

    #ifdef REQUIRE_UV_INTERPOLANT
    OUT.uv = APPLY_TILING_OFFSET(IN.uv, _MainTexture);
    #endif // REQUIRE_UV_INTERPOLANT

    const float3 positionWs = TransformObjectToWorld(IN.vertex);

    OUT.positionCs = TransformWorldToHClip(positionWs);

    UNITY_BRANCH
    if (RenderZeroMotionVectors())
    {
        OUT.positionCsNoJitter = 0;
        OUT.previousPositionCsNoJitter = 0;
    }
    else
    {
        OUT.positionCsNoJitter = mul(_NonJitteredViewProjMatrix, float4(positionWs, 1));

        const float3 previousPosition = UseLastFramePositions() ? IN.positionOld.xyz : IN.vertex;
        OUT.previousPositionCsNoJitter = mul(_PrevViewProjMatrix, mul(UNITY_PREV_MATRIX_M, float4(previousPosition, 1)));   
    }

    ApplyMotionVectorZBias(OUT.positionCs);

    return OUT;
}

float2 PS(const v2f IN) : SV_TARGET
{
    #ifdef _ALPHATEST_ON
    const float alpha = SampleAlbedo(IN.uv).a;
    AlphaClip(alpha);
    #endif // _ALPHATEST_ON

    UNITY_BRANCH
    if (RenderZeroMotionVectors())
    {
        return 0;
    }
    
    return CalcNdcMotionVectorFromCsPositions(IN.positionCsNoJitter, IN.previousPositionCsNoJitter);
}

#endif // TOON_RP_DEFAULT_DEPTH_ONLY_PASS