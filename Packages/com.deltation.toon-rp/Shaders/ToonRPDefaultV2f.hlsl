#ifndef TOON_RP_DEFAULT_V2F
#define TOON_RP_DEFAULT_V2F

#include "../ShaderLibrary/Fog.hlsl"

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

    #ifdef _TOON_RP_ADDITIONAL_LIGHTS_VERTEX
    float3 additionalLights : ADDITIONAL_LIGHTS;
    #endif // _TOON_RP_ADDITIONAL_LIGHTS_VERTEX

    TOON_RP_GI_INTERPOLANT
    TOON_RP_FOG_FACTOR_INTERPOLANT

    float4 positionCs : SV_POSITION;

    UNITY_VERTEX_OUTPUT_STEREO
};

#ifdef _TOON_RP_ADDITIONAL_LIGHTS_VERTEX
#define PER_VERTEX_ADDITIONAL_LIGHTS(IN) (IN.additionalLights)
#else // !_TOON_RP_ADDITIONAL_LIGHTS_VERTEX
#define PER_VERTEX_ADDITIONAL_LIGHTS(IN) (float3(0, 0, 0))
#endif // _TOON_RP_ADDITIONAL_LIGHTS_VERTEX

#endif // TOON_RP_DEFAULT_V2F