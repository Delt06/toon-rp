#ifndef TOON_RP_DEFAULT_FORWARD_PASS
#define TOON_RP_DEFAULT_FORWARD_PASS

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"
#include "../ShaderLibrary/Ramp.hlsl"
#include "../ShaderLibrary/SSAO.hlsl"

#if defined(_TOON_RP_DIRECTIONAL_SHADOWS) || defined(TOON_RP_SSAO_ANY)
#define REQUIRE_DEPTH_INTERPOLANT
#endif // _TOON_RP_DIRECTIONAL_SHADOWS || TOON_RP_ANY

struct appdata
{
    float3 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float3 normalWs : NORMAL_WS;
    float3 positionWs : POSITION_WS;

    #ifdef REQUIRE_DEPTH_INTERPOLANT
    float depth : DEPTH_VS;
    #endif // REQUIRE_DEPTH_INTERPOLANT

    float4 positionCs : SV_POSITION;
};

#include "ToonRpDefaultInput.hlsl"

v2f VS(const appdata IN)
{
    v2f OUT;

    OUT.uv = APPLY_TILING_OFFSET(IN.uv, _MainTexture);
    OUT.normalWs = TransformObjectToWorldNormal(IN.normal);

    const float3 positionWs = TransformObjectToWorld(IN.vertex);
    OUT.positionWs = positionWs;

    #ifdef REQUIRE_DEPTH_INTERPOLANT
    OUT.depth = GetLinearDepth(positionWs);
    #endif // REQUIRE_DEPTH_INTERPOLANT

    const float4 positionCs = TransformWorldToHClip(positionWs);
    OUT.positionCs = positionCs;

    return OUT;
}

float ComputeNDotH(const float3 viewDirectionWs, const float3 normalWs, const float3 lightDirectionWs)
{
    const float3 halfVector = normalize(viewDirectionWs + lightDirectionWs);
    return dot(normalWs, halfVector);
}

float GetShadowAttenuation(const v2f IN, const Light light)
{
    #ifdef _TOON_RP_DIRECTIONAL_SHADOWS
    const float shadowAttenuation = ComputeShadowRamp(light.shadowAttenuation, IN.depth);
    return shadowAttenuation;
    #else // !_TOON_RP_DIRECTIONAL_SHADOWS
    return 1.0f;
    #endif // _TOON_RP_DIRECTIONAL_SHADOWS
}

Light GetMainLight(const v2f IN)
{
    #ifdef _TOON_RP_DIRECTIONAL_SHADOWS
    const float3 shadowCoords = TransformWorldToShadowCoords(IN.positionWs);
    return GetMainLight(shadowCoords); 
    #else // !_TOON_RP_DIRECTIONAL_SHADOWS
    return GetMainLight();
    #endif // _TOON_RP_DIRECTIONAL_SHADOWS
}

float4 PS(const v2f IN) : SV_TARGET
{
    const Light light = GetMainLight(IN);
    const float3 normalWs = normalize(IN.normalWs);
    const float nDotL = dot(normalWs, light.direction);

    float shadowAttenuation = GetShadowAttenuation(IN, light);

    #ifdef TOON_RP_SSAO_ANY
    const float2 screenUv = PositionHClipToScreenUv(IN.positionCs);
    shadowAttenuation *= SampleAmbientOcclusion(screenUv, IN.positionWs, IN.depth);
    #endif // TOON_RP_SSAO_ANY

    float diffuseRamp = ComputeGlobalRamp(nDotL);
    diffuseRamp = min(diffuseRamp * shadowAttenuation, shadowAttenuation);
    const float3 albedo = _MainColor.rgb * SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, IN.uv).rgb;
    const float3 mixedShadowColor = MixShadowColor(albedo, _ShadowColor);
    const float3 diffuse = light.color * ApplyRamp(albedo, mixedShadowColor, diffuseRamp);

    const float3 viewDirectionWs = normalize(GetWorldSpaceViewDir(IN.positionWs));
    const float nDotH = ComputeNDotH(viewDirectionWs, normalWs, light.direction);
    float specularRamp = ComputeGlobalRampSpecular(nDotH);
    specularRamp = min(specularRamp * shadowAttenuation, shadowAttenuation);
    const float3 specular = light.color * _SpecularColor * specularRamp;

    const float3 outputColor = diffuse + specular;
    return float4(outputColor, 1.0f);
}

#endif // TOON_RP_DEFAULT_FORWARD_PASS