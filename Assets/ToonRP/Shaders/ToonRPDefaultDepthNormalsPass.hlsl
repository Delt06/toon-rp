#ifndef TOON_RP_DEFAULT_DEPTH_NORMALS_PASS
#define TOON_RP_DEFAULT_DEPTH_NORMALS_PASS

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/DepthNormals.hlsl"
#include "../ShaderLibrary/NormalMap.hlsl"

#include "ToonRPDefaultInput.hlsl"

struct appdata
{
    float3 vertex : POSITION;
    float3 normal : NORMAL;

    #ifdef _NORMAL_MAP
    float2 uv : TEXCOORD;
    #endif // _NORMAL_MAP

    #ifdef REQUIRE_TANGENT_INTERPOLANT
    float4 tangent : TANGENT;
    #endif // REQUIRE_TANGENT_INTERPOLANT

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 positionCs : SV_POSITION;
    float3 normalWs : NORMAL_WS;

    #ifdef _NORMAL_MAP
    float2 uv : TEXCOORD;
    #endif // _NORMAL_MAP

    #ifdef REQUIRE_TANGENT_INTERPOLANT
    float3 tangentWs : TANGENT_WS;
    float3 bitangentWs : BITANGENT_WS;
    #endif // REQUIRE_TANGENT_INTERPOLANT
};

v2f VS(const appdata IN)
{
    v2f OUT;

    UNITY_SETUP_INSTANCE_ID(IN);

    OUT.positionCs = TransformObjectToHClip(IN.vertex);
    const float3 normalWs = TransformObjectToWorldNormal(IN.normal);
    OUT.normalWs = normalWs;

    #ifdef _NORMAL_MAP
    OUT.uv = APPLY_TILING_OFFSET(IN.uv, _MainTexture);
    #endif // _NORMAL_MAP

    #ifdef REQUIRE_TANGENT_INTERPOLANT
    ComputeTangentsWs(IN.tangent, normalWs, OUT.tangentWs, OUT.bitangentWs);
    #endif // REQUIRE_TANGENT_INTERPOLANT

    return OUT;
}

float4 PS(const v2f IN) : SV_TARGET
{
    #ifdef _NORMAL_MAP
    const float3 normalTs = SampleNormal(IN.uv, _NormalMap, sampler_NormalMap);
    float3 normalWs = TransformTangentToWorld(normalTs, float3x3(IN.tangentWs, IN.bitangentWs, IN.normalWs));
    #else // !_NORMAL_MAP
    float3 normalWs = IN.normalWs;
    #endif // _NORMAL_MAP
    normalWs = normalize(normalWs);

    return float4(PackNormal(normalWs), 0);
}

#endif // TOON_RP_DEFAULT_DEPTH_ONLY_PASS