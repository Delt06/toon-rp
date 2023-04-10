#ifndef TOON_RP_DEFAULT_DEPTH_ONLY_PASS
#define TOON_RP_DEFAULT_DEPTH_ONLY_PASS

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Textures.hlsl"

#if defined(_NORMAL_MAP) || defined(_ALPHATEST_ON)
#define REQUIRE_UV_INTERPOLANT
#endif // _NORMAL_MAP || _ALPHATEST_ON

struct appdata
{
    float3 vertex : POSITION;

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

    float4 positionCs : SV_POSITION;
};

v2f VS(const appdata IN)
{
    v2f OUT;

    UNITY_SETUP_INSTANCE_ID(IN);

    #ifdef REQUIRE_UV_INTERPOLANT
    OUT.uv = APPLY_TILING_OFFSET(IN.uv, _MainTexture);
    #endif // REQUIRE_UV_INTERPOLANT

    OUT.positionCs = TransformObjectToHClip(IN.vertex);

    return OUT;
}

float4 PS(const v2f IN) : SV_TARGET
{
    #ifdef _ALPHATEST_ON
    const float alpha = SampleAlbedo(IN.uv).a;
    AlphaClip(alpha);
    #endif // _ALPHATEST_ON

    return 0;
}

#endif // TOON_RP_DEFAULT_DEPTH_ONLY_PASS