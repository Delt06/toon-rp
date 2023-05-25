#ifndef TOON_RP_DEFAULT_LIT_OUTPUT
#define TOON_RP_DEFAULT_LIT_OUTPUT

#include "ToonRPDefaultInput.hlsl"
#include "ToonRPDefaultV2f.hlsl"

#include "../ShaderLibrary/BlobShadows.hlsl"
#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Fog.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"
#include "../ShaderLibrary/NormalMap.hlsl"
#include "../ShaderLibrary/Matcap.hlsl"
#include "../ShaderLibrary/Ramp.hlsl"
#include "../ShaderLibrary/SSAO.hlsl"

float ComputeNDotH(const float3 viewDirectionWs, const float3 normalWs, const float3 lightDirectionWs)
{
    const float3 halfVector = normalize(viewDirectionWs + lightDirectionWs);
    return dot(normalWs, halfVector);
}

float GetShadowAttenuation(const v2f IN, const Light light)
{
    #if defined(_TOON_RP_ANY_SHADOWS) || defined(_RECEIVE_BLOB_SHADOWS)
    
    float shadowAttenuation = ComputeShadowRamp(light.shadowAttenuation, IN.positionWs);
    #ifdef _TOON_RP_SHADOWS_PATTERN
    const float pattern = SampleShadowPattern(IN.positionWs);
    shadowAttenuation = lerp(shadowAttenuation, 1, pattern);
    #endif // _TOON_RP_SHADOWS_PATTERN
    return shadowAttenuation;

    #else // !_TOON_RP_ANY_SHADOWS && !_TOON_RP_BLOB_SHADOWS

    return 1.0f;

    #endif  // _TOON_RP_ANY_SHADOWS || _TOON_RP_BLOB_SHADOWS
}

Light GetMainLight(const v2f IN)
{
    #ifdef _TOON_RP_SHADOW_MAPS
    const uint tileIndex = ComputeShadowTileIndex(IN.positionWs);
    const float3 shadowCoords = TransformWorldToShadowCoords(IN.positionWs, tileIndex);
    Light light = GetMainLight(shadowCoords);
    #else // !_TOON_RP_SHADOW_MAPS
    Light light = GetMainLight();
    #endif // _TOON_RP_SHADOW_MAPS

    #if defined(_TOON_RP_BLOB_SHADOWS) && defined(_RECEIVE_BLOB_SHADOWS)

    const float blobShadowAttenuation = SampleBlobShadowAttenuation(IN.positionWs);
    light.shadowAttenuation = blobShadowAttenuation;

    #endif // _TOON_RP_BLOB_SHADOWS && _RECEIVE_BLOB_SHADOWS

    return light;
}

float ComputeRampDiffuse(const float nDotL, const v2f IN)
{
    #ifdef _OVERRIDE_RAMP

    const float2 ramp = ConstructOverrideRampDiffuse();
    return ComputeRamp(nDotL, ramp);
    
    #else // !_OVERRIDE_RAMP

    return ComputeGlobalRampDiffuse(nDotL, IN.uv);

    #endif // _OVERRIDE_RAMP
}

float ComputeRampSpecular(const float nDotH, const v2f IN)
{
    #ifdef _OVERRIDE_RAMP

    const float2 ramp = ConstructOverrideRampSpecular();
    return ComputeRamp(nDotH, ramp);
    
    #else // !_OVERRIDE_RAMP

    return ComputeGlobalRampSpecular(nDotH, IN.uv);

    #endif // _OVERRIDE_RAMP
}

float ComputeRampRim(const float fresnel, const v2f IN)
{
    #ifdef _OVERRIDE_RAMP

    const float2 ramp = ConstructOverrideRampRim();
    return ComputeRamp(fresnel, ramp);
    
    #else // !_OVERRIDE_RAMP

    return ComputeGlobalRampRim(fresnel, IN.uv);

    #endif // _OVERRIDE_RAMP
}

float3 ComputeLitOutputColor(const v2f IN, const float4 albedo)
{
    #ifdef _NORMAL_MAP
    const half3 normalTs = SampleNormal(IN.uv, _NormalMap, sampler_NormalMap);
    half3 normalWs = TransformTangentToWorld(normalTs, half3x3(IN.tangentWs, IN.bitangentWs, IN.normalWs));
    #else // !_NORMAL_MAP
    half3 normalWs = IN.normalWs;
    #endif // _NORMAL_MAP
    normalWs = normalize(normalWs);

    const float3 mixedShadowColor = MixShadowColor(albedo.rgb, _ShadowColor);
    const Light light = GetMainLight(IN);
    const float nDotL = dot(normalWs, light.direction);
    float diffuseRamp = ComputeRampDiffuse(nDotL, IN);
    float shadowAttenuation = GetShadowAttenuation(IN, light);

    #ifdef TOON_RP_SSAO_ANY
    const float2 screenUv = PositionHClipToScreenUv(IN.positionCs);
    shadowAttenuation *= SampleAmbientOcclusion(screenUv, IN.positionWs);
    #endif // TOON_RP_SSAO_ANY

    diffuseRamp = min(diffuseRamp * shadowAttenuation, shadowAttenuation);
    const float3 diffuse = ApplyRamp(albedo.rgb, mixedShadowColor, diffuseRamp);

    const float3 viewDirectionWs = normalize(GetWorldSpaceViewDir(IN.positionWs));
    const float nDotH = ComputeNDotH(viewDirectionWs, normalWs, light.direction);
    float specularRamp = ComputeRampSpecular(nDotH, IN);
    specularRamp = min(specularRamp * shadowAttenuation, shadowAttenuation);
    const float3 specular = _SpecularColor * specularRamp;

    const float fresnel = 1 - saturate(dot(viewDirectionWs, normalWs));
    const float rimRamp = ComputeRampRim(fresnel, IN);
    const float3 rim = _RimColor * rimRamp;

    const float3 ambient = SampleSH(normalWs) * albedo.rgb;

    float3 outputColor = light.color * (diffuse + specular) + rim + ambient + _EmissionColor * albedo.a;
    TOON_RP_MATCAP_APPLY_MULTIPLICATIVE(outputColor, IN, _MatcapBlend, _MatcapTint);
    TOON_RP_MATCAP_APPLY_ADDITIVE(outputColor, IN, shadowAttenuation, _MatcapBlend, _MatcapTint);
    return outputColor;
}


#endif // TOON_RP_DEFAULT_LIT_OUTPUT