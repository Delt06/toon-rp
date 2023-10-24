#ifndef TOON_RP_TOON_LIGHTING
#define TOON_RP_TOON_LIGHTING

#include "BlobShadows.hlsl"
#include "Common.hlsl"
#include "Fog.hlsl"
#include "Lighting.hlsl"
#include "NormalMap.hlsl"
#include "Ramp.hlsl"
#include "SSAO.hlsl"
#include "TiledLighting.hlsl"

float ComputeNDotH(const float3 viewDirectionWs, const float3 normalWs, const float3 lightDirectionWs)
{
    const float3 halfVector = normalize(viewDirectionWs + lightDirectionWs);
    return dot(normalWs, halfVector);
}

struct LightComputationParameters
{
    float4 positionCs;
    float3 positionWs;
    float2 globalRampUv;
    float3 normalWs;
    float3 viewDirectionWs;
    float3 perVertexAdditionalLights;
    
    float4 albedo;
    float4 shadowColor;
    float3 specularColor;
    float specularSizeOffset;

    float2 overrideRampDiffuse;
    float2 overrideRampSpecular;
    float2 overrideRampRim;
};

float ComputeRampDiffuse(const LightComputationParameters parameters, const float nDotL)
{
    #ifdef _OVERRIDE_RAMP

    return ComputeRamp(nDotL, parameters.overrideRampDiffuse);
    
    #else // !_OVERRIDE_RAMP

    return ComputeGlobalRampDiffuse(nDotL, parameters.globalRampUv);

    #endif // _OVERRIDE_RAMP
}

float ComputeRampSpecular(const LightComputationParameters parameters, const float nDotH)
{
    #ifdef _OVERRIDE_RAMP

    return ComputeRamp(nDotH, parameters.overrideRampSpecular);
    
    #else // !_OVERRIDE_RAMP

    return ComputeGlobalRampSpecular(nDotH, parameters.globalRampUv);

    #endif // _OVERRIDE_RAMP
}

float ComputeRampRim(const LightComputationParameters parameters, const float fresnel)
{
    #ifdef _OVERRIDE_RAMP

    return ComputeRamp(fresnel, parameters.overrideRampRim);
    
    #else // !_OVERRIDE_RAMP

    return ComputeGlobalRampRim(fresnel, parameters.globalRampUv);

    #endif // _OVERRIDE_RAMP
}

float GetSsao(const LightComputationParameters parameters)
{
    #ifdef TOON_RP_SSAO_ANY
    const float2 screenUv = PositionHClipToScreenUv(parameters.positionCs);
    return SampleAmbientOcclusion(screenUv, parameters.positionWs);;
    #else // !TOON_RP_SSAO_ANY
    return 1.0f;
    #endif // TOON_RP_SSAO_ANY
}

float GetShadowAttenuation(const LightComputationParameters parameters, const Light light)
{
    #if defined(_TOON_RP_BLOB_SHADOWS) && !defined(_RECEIVE_BLOB_SHADOWS)
    return 1.0f;
    #endif // _TOON_RP_BLOB_SHADOWS && _RECEIVE_BLOB_SHADOWS
    
    #if defined(_TOON_RP_ANY_SHADOWS)
    
    float shadowAttenuation = ComputeShadowRamp(light.shadowAttenuation, parameters.positionWs);
    #ifdef _TOON_RP_SHADOWS_PATTERN
    const float pattern = SampleShadowPattern(parameters.positionWs);
    shadowAttenuation = lerp(shadowAttenuation, 1, pattern);
    #endif // _TOON_RP_SHADOWS_PATTERN
    return shadowAttenuation;

    #else // !_TOON_RP_ANY_SHADOWS

    return 1.0f;

    #endif  // _TOON_RP_ANY_SHADOWS
}

Light GetMainLight(const LightComputationParameters parameters)
{
    #ifdef _TOON_RP_SHADOW_MAPS
    const uint tileIndex = ComputeShadowTileIndex(parameters.positionWs);
    const float3 shadowCoords = TransformWorldToShadowCoords(parameters.positionWs, tileIndex);
    Light light = GetMainLight(shadowCoords, parameters.positionWs);
    #else // !_TOON_RP_SHADOW_MAPS
    Light light = GetMainLight();
    #endif // _TOON_RP_SHADOW_MAPS

    #if defined(_TOON_RP_BLOB_SHADOWS) && defined(_RECEIVE_BLOB_SHADOWS)

    const float blobShadowAttenuation = SampleBlobShadowAttenuation(parameters.positionWs);
    light.shadowAttenuation = blobShadowAttenuation;

    #endif // _TOON_RP_BLOB_SHADOWS && _RECEIVE_BLOB_SHADOWS

    return light;
}

float3 ComputeMainLightComponent(const in LightComputationParameters parameters, const float ssao,
                                 out float shadowAttenuation)
{
    const float3 mixedShadowColor = MixShadowColor(parameters.albedo.rgb, parameters.shadowColor);
    const Light light = GetMainLight(parameters);
    const float nDotL = dot(parameters.normalWs, light.direction);
    float diffuseRamp = ComputeRampDiffuse(parameters, nDotL);
    #if _RECEIVE_SHADOWS_OFF
    shadowAttenuation = 1.0f;
    #else // !_RECEIVE_SHADOWS_OFF
    shadowAttenuation = GetShadowAttenuation(parameters, light);
    #endif // _RECEIVE_SHADOWS_OFF
    shadowAttenuation *= ssao;

    diffuseRamp = min(diffuseRamp * shadowAttenuation, shadowAttenuation);
    const float3 diffuse = ApplyRamp(parameters.albedo.rgb, mixedShadowColor, diffuseRamp);

    #ifdef _TOON_LIGHTING_SPECULAR
    const float nDotH = ComputeNDotH(parameters.viewDirectionWs, parameters.normalWs, light.direction);
    float specularRamp = ComputeRampSpecular(parameters, nDotH + parameters.specularSizeOffset);
    specularRamp = min(specularRamp * shadowAttenuation, shadowAttenuation);
    const float3 specular = parameters.specularColor * specularRamp;
    #else // !_TOON_LIGHTING_SPECULAR
    const float3 specular = 0;
    #endif // _TOON_LIGHTING_SPECULAR

    return light.color * (diffuse + specular);
}

float3 ComputeAdditionalLightsRawDiffuse(const LightComputationParameters parameters, const float ssao)
{
    #ifdef _TOON_RP_TILED_LIGHTING
    TiledLighting_LightGridCell cell = TiledLighting_GetLightGridCell(parameters.positionCs.xy);
    const uint lightCount = cell.lightCount;
    #else // !_TOON_RP_TILED_LIGHTING
    const uint lightCount = GetPerObjectAdditionalLightCount();
    #endif // _TOON_RP_TILED_LIGHTING 
    float3 lights = 0;

    UNITY_LOOP
    for (uint i = 0; i < lightCount; ++i)
    {
        #ifdef _TOON_RP_TILED_LIGHTING
        const Light light = GetAdditionalLightTiled(i, cell, parameters.positionWs);
        #else // !_TOON_RP_TILED_LIGHTING
        const Light light = GetAdditionalLight(i, parameters.positionWs);
        #endif // _TOON_RP_TILED_LIGHTING
        float nDotL = dot(parameters.normalWs, light.direction);
        const float attenuation = light.distanceAttenuation * ssao;
        nDotL = min(nDotL * attenuation, attenuation);
        const float rampCutoff = step(0.00001f, nDotL);

        #ifdef _TOON_RP_ADDITIONAL_LIGHTS_VERTEX
        const float diffuseRamp = saturate(nDotL);
        #else // !_TOON_RP_ADDITIONAL_LIGHTS_VERTEX
        const float diffuseRamp = ComputeRampDiffuse(parameters, nDotL) * rampCutoff;
        #endif  // _TOON_RP_ADDITIONAL_LIGHTS_VERTEX

        lights += diffuseRamp * step(0.001, attenuation) * light.color;
    }

    return lights;
}

float3 ComputeAdditionalLightComponent(const in LightComputationParameters parameters, const float ssao)
{
    const float3 rawDiffuse = ComputeAdditionalLightsRawDiffuse(
        parameters,
        ssao);
    return rawDiffuse * parameters.albedo.rgb;
}

float3 ComputeAdditionalLightComponentPerVertex(const in LightComputationParameters parameters)
{
    const float3 rawDiffuse = parameters.perVertexAdditionalLights;
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

#endif // TOON_RP_TOON_LIGHTING
