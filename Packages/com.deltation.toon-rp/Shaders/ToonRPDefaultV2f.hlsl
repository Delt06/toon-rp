#ifndef TOON_RP_DEFAULT_V2F
#define TOON_RP_DEFAULT_V2F

#include "../ShaderLibrary/Fog.hlsl"
#include "../ShaderLibrary/Matcap.hlsl"

struct v2f
{
    float2 uv : TEXCOORD0;
    #if !defined(UNLIT)
    half3 normalWs : NORMAL_WS;
    float3 positionWs : POSITION_WS;
    #endif // !UNLIT

    #ifdef REQUIRE_TANGENT_INTERPOLANT
    half3 tangentWs : TANGENT_WS;
    half3 bitangentWs : BITANGENT_WS;
    #endif // REQUIRE_TANGENT_INTERPOLANT

    TOON_RP_FOG_FACTOR_INTERPOLANT
    TOON_RP_MATCAP_UV_INTERPOLANT

    float4 positionCs : SV_POSITION;
};


#endif // TOON_RP_DEFAULT_V2F