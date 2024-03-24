#ifndef TOON_RP_DEFAULT_DEPTH_NORMALS_PASS
#define TOON_RP_DEFAULT_DEPTH_NORMALS_PASS

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/DepthNormals.hlsl"
#include "../ShaderLibrary/NormalMap.hlsl"
#include "../ShaderLibrary/Textures.hlsl"

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

    #ifdef REQUIRE_TANGENT_INTERPOLANT
    float4 tangent : TANGENT;
    #endif // REQUIRE_TANGENT_INTERPOLANT

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 positionCs : SV_POSITION;
    float3 normalWs : NORMAL_WS;

    #ifdef REQUIRE_UV_INTERPOLANT
    float2 uv : TEXCOORD;
    #endif // REQUIRE_UV_INTERPOLANT

    #ifdef REQUIRE_TANGENT_INTERPOLANT
    float3 tangentWs : TANGENT_WS;
    float3 bitangentWs : BITANGENT_WS;
    #endif // REQUIRE_TANGENT_INTERPOLANT

    UNITY_VERTEX_OUTPUT_STEREO
};

v2f VS(const appdata IN)
{
    v2f OUT;

    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

    OUT.positionCs = TransformObjectToHClip(IN.vertex);
    const float3 normalWs = TransformObjectToWorldNormal(IN.normal);
    OUT.normalWs = normalWs;

    #ifdef REQUIRE_UV_INTERPOLANT
    OUT.uv = APPLY_TILING_OFFSET(IN.uv, _MainTexture);
    #endif // REQUIRE_UV_INTERPOLANT

    #ifdef REQUIRE_TANGENT_INTERPOLANT
    ComputeTangentsWs(IN.tangent, normalWs, OUT.tangentWs, OUT.bitangentWs);
    #endif // REQUIRE_TANGENT_INTERPOLANT

    return OUT;
}

float2 PS(const v2f IN) : SV_TARGET
{
    #ifdef _ALPHATEST_ON
    const float alpha = SampleAlbedo(IN.uv).a;
    AlphaClip(alpha);
    #endif // _ALPHATEST_ON

    #ifdef _NORMAL_MAP
    const half3 normalTs = SampleNormal(IN.uv, _NormalMap, sampler_NormalMap);
    half3 normalWs = TransformTangentToWorld(normalTs, half3x3(IN.tangentWs, IN.bitangentWs, IN.normalWs));
    #else // !_NORMAL_MAP
    half3 normalWs = IN.normalWs;
    #endif // _NORMAL_MAP
    normalWs = normalize(normalWs);

    return PackNormal(normalWs);
}

#endif // TOON_RP_DEFAULT_DEPTH_ONLY_PASS