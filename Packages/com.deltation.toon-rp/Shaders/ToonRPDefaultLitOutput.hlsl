#ifndef TOON_RP_DEFAULT_LIT_OUTPUT
#define TOON_RP_DEFAULT_LIT_OUTPUT

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

float ComputeRampDiffuse(const float nDotL, const float2 uv)
{
    #ifdef _OVERRIDE_RAMP

    const float2 ramp = ConstructOverrideRampDiffuse();
    return ComputeRamp(nDotL, ramp);
    
    #else // !_OVERRIDE_RAMP

    return ComputeGlobalRampDiffuse(nDotL, uv);

    #endif // _OVERRIDE_RAMP
}

float ComputeRampSpecular(const float nDotH, const float2 uv)
{
    #ifdef _OVERRIDE_RAMP

    const float2 ramp = ConstructOverrideRampSpecular();
    return ComputeRamp(nDotH, ramp);
    
    #else // !_OVERRIDE_RAMP

    return ComputeGlobalRampSpecular(nDotH, uv);

    #endif // _OVERRIDE_RAMP
}

float ComputeRampRim(const float fresnel, const float2 uv)
{
    #ifdef _OVERRIDE_RAMP

    const float2 ramp = ConstructOverrideRampRim();
    return ComputeRamp(fresnel, ramp);
    
    #else // !_OVERRIDE_RAMP

    return ComputeGlobalRampRim(fresnel, uv);

    #endif // _OVERRIDE_RAMP
}

struct LightComputationParameters
{
    v2f IN;
    float4 albedo;
    float3 normalWs;
    float3 viewDirectionWs;
};

float GetSsao(in LightComputationParameters parameters)
{
    #ifdef TOON_RP_SSAO_ANY
    const float2 screenUv = PositionHClipToScreenUv(parameters.IN.positionCs);
    return SampleAmbientOcclusion(screenUv, parameters.IN.positionWs);;
    #else // !TOON_RP_SSAO_ANY
    return 1.0f;
    #endif // TOON_RP_SSAO_ANY
}

float3 ComputeMainLightComponent(const in LightComputationParameters parameters, const float ssao,
                                 out float shadowAttenuation)
{
    const float3 mixedShadowColor = MixShadowColor(parameters.albedo.rgb, _ShadowColor);
    const Light light = GetMainLight(parameters.IN);
    const float nDotL = dot(parameters.normalWs, light.direction);
    float diffuseRamp = ComputeRampDiffuse(nDotL, parameters.IN.uv);
    shadowAttenuation = GetShadowAttenuation(parameters.IN, light);
    shadowAttenuation *= ssao;

    diffuseRamp = min(diffuseRamp * shadowAttenuation, shadowAttenuation);
    const float3 diffuse = ApplyRamp(parameters.albedo.rgb, mixedShadowColor, diffuseRamp);

    const float nDotH = ComputeNDotH(parameters.viewDirectionWs, parameters.normalWs, light.direction);
    float specularRamp = ComputeRampSpecular(nDotH, parameters.IN.uv);
    specularRamp = min(specularRamp * shadowAttenuation, shadowAttenuation);

    #ifdef SPECULAR
    const float3 specular = _SpecularColor * specularRamp;
    #else // !SPECULAR
    const float3 specular = 0;
    #endif // SPECULAR

    return light.color * (diffuse + specular);
}

float3 ComputeAdditionalLightsRawDiffuse(const float3 positionWs, const half3 normalWs, const float2 uv,
                                         const float ssao)
{
    const uint lightsCount = GetPerObjectAdditionalLightCount();
    float3 lights = 0;

    for (uint i = 0; i < lightsCount; ++i)
    {
        const Light light = GetAdditionalLight(i, positionWs);
        float nDotL = dot(normalWs, light.direction);
        const float attenuation = light.distanceAttenuation * ssao;
        nDotL = min(nDotL * attenuation, attenuation);

        #ifdef _TOON_RP_ADDITIONAL_LIGHTS_VERTEX
        const float diffuseRamp = saturate(nDotL);
        #else // !_TOON_RP_ADDITIONAL_LIGHTS_VERTEX
        const float diffuseRamp = ComputeRampDiffuse(nDotL, uv);
        #endif  // _TOON_RP_ADDITIONAL_LIGHTS_VERTEX

        lights += diffuseRamp * step(0.001, attenuation) * light.color;
    }

    return lights;
}

float3 ComputeAdditionalLightComponent(const in LightComputationParameters parameters, const float ssao)
{
    const float3 rawDiffuse = ComputeAdditionalLightsRawDiffuse(
        parameters.IN.positionWs,
        parameters.IN.normalWs, parameters.IN.uv,
        ssao);
    return rawDiffuse * parameters.albedo.rgb;
}

float3 ComputeAdditionalLightComponentPerVertex(const in LightComputationParameters parameters)
{
    const float3 rawDiffuse = PER_VERTEX_ADDITIONAL_LIGHTS(parameters.IN);
    return rawDiffuse * parameters.albedo.rgb;
}

float3 ComputeLights(const in LightComputationParameters parameters, out float outShadowAttenuation)
{
    const float ssao = GetSsao(parameters);
    float3 lights = ComputeMainLightComponent(parameters, ssao, outShadowAttenuation);

    #if defined(_TOON_RP_ADDITIONAL_LIGHTS)
    lights += ComputeAdditionalLightComponent(parameters, ssao);
    #elif defined(_TOON_RP_ADDITIONAL_LIGHTS_VERTEX)
    lights += ComputeAdditionalLightComponentPerVertex(parameters);
    #endif

    return lights;
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

    const float3 viewDirectionWs = normalize(GetWorldSpaceViewDir(IN.positionWs));
    LightComputationParameters lightComputationParameters;
    lightComputationParameters.IN = IN;
    lightComputationParameters.albedo = albedo;
    lightComputationParameters.normalWs = normalWs;
    lightComputationParameters.viewDirectionWs = viewDirectionWs;
    // ReSharper disable once CppEntityAssignedButNoRead
    float shadowAttenuation;
    const float3 lights = ComputeLights(lightComputationParameters, shadowAttenuation);

    const float fresnel = 1 - saturate(dot(viewDirectionWs, normalWs));
    const float rimRamp = ComputeRampRim(fresnel, IN.uv);
    #ifdef RIM
    const float3 rim = _RimColor * rimRamp;
    #else // !RIM
    const float3 rim = 0;
    #endif // RIM

    #ifdef _FORCE_DISABLE_ENVIRONMENT_LIGHT
    const float3 ambient = 0;
    #else // !_FORCE_DISABLE_ENVIRONMENT_LIGHT
    const float3 ambient = SampleSH(normalWs) * albedo.rgb;
    #endif // _FORCE_DISABLE_ENVIRONMENT_LIGHT

    #ifdef EMISSION
    const float3 emission = _EmissionColor * albedo.a;
    #else // !EMISSION
    const float3 emission = 0;
    #endif // EMISSION

    float3 outputColor = lights + rim + ambient + emission;
    TOON_RP_MATCAP_APPLY_MULTIPLICATIVE(outputColor, IN, _MatcapBlend, _MatcapTint);
    TOON_RP_MATCAP_APPLY_ADDITIVE(outputColor, IN, shadowAttenuation, _MatcapBlend, _MatcapTint);
    return outputColor;
}


#endif // TOON_RP_DEFAULT_LIT_OUTPUT