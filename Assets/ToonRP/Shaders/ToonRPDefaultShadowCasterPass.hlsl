#ifndef TOON_RP_DEFAULT_SHADOW_CASTER_PASS
#define TOON_RP_DEFAULT_SHADOW_CASTER_PASS

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"

#include "ToonRpDefaultInput.hlsl"

#if defined(_NORMAL_MAP) || defined(_ALPHATEST_ON)
#define REQUIRE_UV_INTERPOLANT
#endif // _NORMAL_MAP || _ALPHATEST_ON

struct appdata
{
    float3 vertex : POSITION;
    float3 normal : NORMAL;

    #ifdef REQUIRE_UV_INTERPOLANT
    float2 uv : TEXCOORD;
    #endif // REQUIRE_UV_INTERPOLANT

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    #ifdef REQUIRE_UV_INTERPOLANT
    float2 uv : TEXCOORD;
    #endif // REQUIRE_UV_INTERPOLANT

    float depth : VIEW_Z;

    float4 positionCs : SV_POSITION;
};

v2f VS(const appdata IN)
{
    v2f OUT;

    UNITY_SETUP_INSTANCE_ID(IN);

    #ifdef REQUIRE_UV_INTERPOLANT
    OUT.uv = APPLY_TILING_OFFSET(IN.uv, _MainTexture);
    #endif // REQUIRE_UV_INTERPOLANT

    float3 positionWs = TransformObjectToWorld(IN.vertex);
    const float3 normalWs = TransformObjectToWorldNormal(IN.normal);
    // TODO: if in point light shadow pass, use a different light direction
    positionWs = ApplyShadowBias(positionWs, normalWs, _DirectionalLightDirection);
    OUT.positionCs = TransformWorldToHClip(positionWs);

    float viewZ = TransformWorldToView(positionWs).z;
    #ifdef UNITY_REVERSED_Z
    viewZ *= -1.0f;
    #endif // UNITY_REVERSED_Z
    OUT.depth = PackVsmDepth(viewZ);

    return OUT;
}

struct PsOut
{
    float2 depth_depthSqr : SV_TARGET;
};

PsOut PS(const v2f IN)
{
    #ifdef _ALPHATEST_ON
    const float alpha = SampleAlbedo(IN.uv).a;
    AlphaClip(alpha);
    #endif // _ALPHATEST_ON

    PsOut OUT;
    OUT.depth_depthSqr = float2(IN.depth, IN.depth * IN.depth);
    return OUT;
}

#endif // TOON_RP_DEFAULT_SHADOW_CASTER_PASS