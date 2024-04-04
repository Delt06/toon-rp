#ifndef TOON_RP_DEFAULT_FORWARD_PASS
#define TOON_RP_DEFAULT_FORWARD_PASS

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Fog.hlsl"
#include "../ShaderLibrary/ToonLighting.hlsl"

#ifdef UNLIT

#include "ToonRPUnlitInput.hlsl"
#include "ToonRPDefaultV2f.hlsl"

#else // !UNLIT

#ifdef DEFAULT_LITE
#include "ToonRPDefaultLiteInput.hlsl"
#else // !DEFAULT_LITE
#include "ToonRPDefaultInput.hlsl"
#endif // DEFAULT_LITE

#include "ToonRPDefaultV2f.hlsl"
#include "ToonRPDefaultLitOutput.hlsl"

#endif // UNLIT

struct appdata
{
    float3 vertex : POSITION;
    #if !defined(UNLIT)
    half3 normal : NORMAL;
    #endif // !UNLIT
    float2 uv : TEXCOORD0;

    #ifdef REQUIRE_TANGENT_INTERPOLANT
    half4 tangent : TANGENT;
    #endif // REQUIRE_TANGENT_INTERPOLANT

    TOON_RP_GI_ATTRIBUTE
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

v2f VS(const appdata IN)
{
    v2f OUT;

    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

    const float2 uv = APPLY_TILING_OFFSET(IN.uv, _MainTexture);
    OUT.uv = uv;

    const float3 positionWs = TransformObjectToWorld(IN.vertex);

    #if !defined(UNLIT)

    const half3 normalWs = TransformObjectToWorldNormal(IN.normal);
    OUT.normalWs = normalWs;
    OUT.positionWs = positionWs;

    #endif // !UNLIT

    const float4 positionCs = TransformWorldToHClip(positionWs);
    OUT.positionCs = positionCs;

    #ifdef REQUIRE_TANGENT_INTERPOLANT
    ComputeTangentsWs(IN.tangent, normalWs, OUT.tangentWs, OUT.bitangentWs);
    #endif // REQUIRE_TANGENT_INTERPOLANT

    #ifdef _TOON_RP_ADDITIONAL_LIGHTS_VERTEX
    LightComputationParameters lightComputationParameters = (LightComputationParameters) 0;
    lightComputationParameters.positionWs = positionWs;
    lightComputationParameters.positionCs = positionCs;
    lightComputationParameters.normalWs = normalWs;

    float3 additionalLightsSpecularUnused;
    ComputeAdditionalLightsDiffuseSpecular(lightComputationParameters, 1, OUT.additionalLights, additionalLightsSpecularUnused);
    #endif // _TOON_RP_ADDITIONAL_LIGHTS_VERTEX

    TOON_RP_GI_TRANSFER(IN, OUT);
    TOON_RP_FOG_FACTOR_TRANSFER(OUT, positionCs);

    return OUT;
}

float4 PS(const v2f IN) : SV_TARGET
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
    
    float4 albedo = SampleAlbedo(IN.uv);
    AlphaClip(albedo);

    #ifdef _ALPHAPREMULTIPLY_ON
    albedo.rgb *= albedo.a;
    #endif // _ALPHAPREMULTIPLY_ON

    #ifdef UNLIT
    float3 outputColor = albedo.rgb;
    #else // !UNLIT
    float3 outputColor = ComputeLitOutputColor(IN, albedo);
    #endif // UNLIT

    TOON_RP_FOG_MIX(IN, outputColor);

    return float4(outputColor, albedo.a);
}

#endif // TOON_RP_DEFAULT_FORWARD_PASS