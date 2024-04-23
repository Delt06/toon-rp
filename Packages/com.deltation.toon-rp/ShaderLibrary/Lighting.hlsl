#ifndef TOON_RP_LIGHTING
#define TOON_RP_LIGHTING

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"

#include "LightingDefines.hlsl"
#include "Shadows.hlsl"
#include "UnityInput.hlsl"

#define MAX_ADDITIONAL_LIGHT_COUNT 64
#define MAX_ADDITIONAL_LIGHTS_PER_OBJECT 4

CBUFFER_START(_ToonRPLight)
    float3 _DirectionalLightColor;
    float3 _DirectionalLightDirection;
    half4 _DirectionalLightOcclusionProbes;

    uint _AdditionalLightCount;
    float4 _AdditionalLightPositions[MAX_ADDITIONAL_LIGHT_COUNT]; // xyz = position, w = spotAttenuation.x
    half4 _AdditionalLightColors[MAX_ADDITIONAL_LIGHT_COUNT]; // xyz = color, w = 1/range^2
    half4 _AdditionalLightSpotDir[MAX_ADDITIONAL_LIGHT_COUNT]; // xyz - spot dir, w = spotAttenuation.y

    float3 _AdditionalLightRampOffset; // x - diffuse, y - specular, z - attenuation factor
CBUFFER_END

struct Light
{
    float3 color;
    float3 direction;
    float shadowAttenuation;
    float distanceAttenuation;
};

Light GetMainLight()
{
    Light light;
    light.color = _DirectionalLightColor;
    light.direction = _DirectionalLightDirection;
    light.shadowAttenuation = 1.0f;
    light.distanceAttenuation = 1.0f;
    return light;
}

Light GetMainLight(const float3 shadowCoords, const float3 positionWs)
{
    Light light;
    light.color = _DirectionalLightColor;
    light.direction = _DirectionalLightDirection;
    
    #ifdef _TOON_RP_SHADOW_MAPS
    light.shadowAttenuation = SampleShadowAttenuation(shadowCoords, positionWs);
    #else // !_TOON_RP_SHADOW_MAPS
    light.shadowAttenuation = 1.0f;
    #endif // _TOON_RP_SHADOW_MAPS
    
    light.distanceAttenuation = 1.0f;
    return light;
}

half GetMainLightShadowStrength()
{
    // TODO: https://github.com/Delt06/toon-rp/issues/230
    return 1.h;
}

uint GetPerObjectAdditionalLightCount()
{
    return min((uint)unity_LightData.y, MAX_ADDITIONAL_LIGHTS_PER_OBJECT);
}

uint ToGlobalLightIndex(const uint perObjectIndex)
{
    // Take the "vec4" part into float4 tmp variable in order to force float4 math.
    // It appears indexing half4 as min16float4 on DX11 can fail. (dp4 {min16f})
    const float4 tmp = unity_LightIndices[perObjectIndex / 4];
    return uint(tmp[perObjectIndex % 4]);
}

struct LightEntry
{
    float3 color;
    float3 positionWs;
    float distanceAttenuation;
    half3 spotDir;
    half2 spotAttenuation;
    int shadowIndex;
};

int GetLightShadowIndex(const uint globalLightIndex)
{
    if (globalLightIndex < MAX_ADDITIONAL_LIGHT_SHADOWS_COUNT)
    {
        const half4 metadata = _ToonRP_AdditionalShadows_Metadata[globalLightIndex];
        return metadata.x;
    }
    
    return -1;
}

LightEntry GetUniformLightEntry(const uint globalLightIndex)
{
    LightEntry lightEntry;

    {
        const half4 color_distanceAttenuation = _AdditionalLightColors[globalLightIndex];
        lightEntry.color = color_distanceAttenuation.xyz;
        lightEntry.distanceAttenuation = color_distanceAttenuation.w;
    }

    {
        const float4 positionWs_spotAttenuation = _AdditionalLightPositions[globalLightIndex];
        lightEntry.positionWs = positionWs_spotAttenuation.xyz;
        lightEntry.spotAttenuation.x = positionWs_spotAttenuation.w;    
    }

    {
        const half4 spotDir_spotAttenuation = _AdditionalLightSpotDir[globalLightIndex];
        lightEntry.spotDir = spotDir_spotAttenuation.xyz;
        lightEntry.spotAttenuation.y = spotDir_spotAttenuation.w;
    }

    lightEntry.shadowIndex = GetLightShadowIndex(globalLightIndex);
    
    return lightEntry;
}


float DistanceAttenuation(const float3 offset, const float distanceAttenuationParam)
{
    const float distanceSqr = max(dot(offset, offset), 0.00001);
    const float distanceAttenuation = Sq(
        saturate(1.0f - Sq(distanceSqr * distanceAttenuationParam))
    );
    return distanceAttenuation * rcp(distanceSqr);
}

half AngleAttenuation(const half3 spotDirection, const half3 lightDirection, const half2 spotAttenuationParams)
{
    // From URP's RealtimeLights.hlsl
    
    // Spot Attenuation with a linear falloff can be defined as
    // (SdotL - cosOuterAngle) / (cosInnerAngle - cosOuterAngle)
    // This can be rewritten as
    // invAngleRange = 1.0 / (cosInnerAngle - cosOuterAngle)
    // SdotL * invAngleRange + (-cosOuterAngle * invAngleRange)
    // SdotL * spotAttenuation.x + spotAttenuation.y

    // if (spotAttenuationParams.x == 0.0h)
    //     return 1.0h;

    // If we precompute the terms in a MAD instruction
    const half SdotL = dot(spotDirection, lightDirection);
    const half atten = saturate(SdotL * spotAttenuationParams.x + spotAttenuationParams.y);
    return atten * atten;
}

Light ConvertEntryToLight(const LightEntry lightEntry, const float3 positionWs)
{
    Light light;

    light.color = lightEntry.color;
    const float3 offset = lightEntry.positionWs - positionWs;
    light.direction = normalize(offset);

#ifdef _TOON_RP_ADDITIONAL_SHADOWS
    UNITY_BRANCH
    if (lightEntry.shadowIndex >= 0)
    {
        const float3 shadowCoords = TransformWorldToAdditionalShadowCoords(positionWs, lightEntry.shadowIndex);
        light.shadowAttenuation = SampleAdditionalShadowAttenuation(shadowCoords, lightEntry.shadowIndex);
    }
    else
#endif // _TOON_RP_ADDITIONAL_SHADOWS
    {
        light.shadowAttenuation = 1.0f;    
    }
    

    light.distanceAttenuation = DistanceAttenuation(offset, lightEntry.distanceAttenuation) *
        AngleAttenuation(lightEntry.spotDir, light.direction, lightEntry.spotAttenuation);
    light.distanceAttenuation = saturate(light.distanceAttenuation * _AdditionalLightRampOffset.z);

    return light;
}

Light GetAdditionalLight(const uint perObjectIndex, const float3 positionWs)
{
    const uint globalIndex = ToGlobalLightIndex(perObjectIndex);
    const LightEntry lightEntry = GetUniformLightEntry(globalIndex);
    return ConvertEntryToLight(lightEntry, positionWs);
}

// Samples SH L0, L1 and L2 terms
float3 SampleSH(const float3 normalWs)
{
    real4 shCoefficients[7];
    shCoefficients[0] = unity_SHAr;
    shCoefficients[1] = unity_SHAg;
    shCoefficients[2] = unity_SHAb;
    shCoefficients[3] = unity_SHBr;
    shCoefficients[4] = unity_SHBg;
    shCoefficients[5] = unity_SHBb;
    shCoefficients[6] = unity_SHC;

    return max(float3(0, 0, 0), SampleSH9(shCoefficients, normalWs));
}

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);

TEXTURE2D(unity_LightmapInd);
TEXTURE2D_ARRAY(unity_LightmapsInd);

TEXTURE2D(unity_ShadowMask);
SAMPLER(samplerunity_ShadowMask);

float4 _SubtractiveShadowColor;

#if !defined(_MIXED_LIGHTING_SUBTRACTIVE) && defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK)
#define _MIXED_LIGHTING_SUBTRACTIVE
#endif

#if defined(LIGHTMAP_ON) || defined(LIGHTMAP_SHADOW_MIXING) || defined(SHADOWS_SHADOWMASK)
#define CALCULATE_BAKED_SHADOWS
#endif

#if defined(LIGHTMAP_ON)
#define TOON_RP_GI_ATTRIBUTE float2 lightmapUv : TEXCOORD1;
#define TOON_RP_GI_INTERPOLANT float2 lightmapUv : TOON_RP_LIGHTMAP_UV;
#define TOON_RP_GI_TRANSFER_ATT(input, output, attributeName) \
    output.lightmapUv = input.attributeName * \
    unity_LightmapST.xy + unity_LightmapST.zw;
#define TOON_RP_GI_TRANSFER(input, output) TOON_RP_GI_TRANSFER_ATT(input, output, lightmapUv)
#define TOON_RP_GI_FRAGMENT_DATA(input) input.lightmapUv
#else // !LIGHTMAP_ON
#define TOON_RP_GI_ATTRIBUTE
#define TOON_RP_GI_INTERPOLANT
#define TOON_RP_GI_TRANSFER_ATT(input, output, attributeName)
#define TOON_RP_GI_TRANSFER(input, output)
#define TOON_RP_GI_FRAGMENT_DATA(input) 0.0
#endif // LIGHTMAP_ON

#define LIGHTMAP_NAME unity_Lightmap
#define LIGHTMAP_INDIRECTION_NAME unity_LightmapInd
#define LIGHTMAP_SAMPLER_NAME samplerunity_Lightmap

#define SHADOWMASK_NAME unity_ShadowMask
#define SHADOWMASK_SAMPLER_NAME samplerunity_ShadowMask
#define SHADOWMASK_SAMPLE_EXTRA_ARGS

#if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
#define SAMPLE_SHADOWMASK(uv) (SAMPLE_TEXTURE2D_LIGHTMAP(SHADOWMASK_NAME, SHADOWMASK_SAMPLER_NAME, uv SHADOWMASK_SAMPLE_EXTRA_ARGS))
#elif !defined (LIGHTMAP_ON)
#define SAMPLE_SHADOWMASK(uv) (unity_ProbesOcclusion)
#else
#define SAMPLE_SHADOWMASK(uv) (half4(1, 1, 1, 1))
#endif

float3 SampleLightmap(const float2 lightmapUv, const half3 normalWs)
{
#if defined(LIGHTMAP_ON)
    #ifdef UNITY_LIGHTMAP_FULL_HDR
    bool encodedLightmap = false;
    #else
    bool encodedLightmap = true;
    #endif

    half4 decodeInstructions = half4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0h, 0.0h);

    // The shader library sample lightmap functions transform the lightmap uv coords to apply bias and scale.
    // However, universal pipeline already transformed those coords in vertex. We pass half4(1, 1, 0, 0) and
    // the compiler will optimize the transform away.
    half4 transformCoords = half4(1, 1, 0, 0);

    float3 diffuseLighting = 0;

    #if defined(LIGHTMAP_ON) && defined(DIRLIGHTMAP_COMBINED)
    diffuseLighting = SampleDirectionalLightmap(TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_NAME, LIGHTMAP_SAMPLER_NAME),
        TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_INDIRECTION_NAME, LIGHTMAP_SAMPLER_NAME),
        lightmapUv, transformCoords, normalWs, encodedLightmap, decodeInstructions);
    #elif defined(LIGHTMAP_ON)
    diffuseLighting = SampleSingleLightmap(TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_NAME, LIGHTMAP_SAMPLER_NAME), lightmapUv, transformCoords, encodedLightmap, decodeInstructions);
    #endif
    return diffuseLighting;
#else // !LIGHTMAP_ON
    return 0.0;
#endif // LIGHTMAP_ON
}

float3 SampleLightProbe(const half3 normalWs)
{
#if defined(LIGHTMAP_ON)
    return 0.0;
#else // !LIGHTMAP_ON
    return SampleSH(normalWs);
#endif // LIGHTMAP_ON
}

float3 ComputeBakedGi(const float2 lightmapUv, const half3 normalWs)
{
    return SampleLightmap(lightmapUv, normalWs) + SampleLightProbe(normalWs);
}

float3 SubtractDirectMainLightFromLightmap(const float3 bakedGi, const float mainLightDiffuseRamp, const float mainLightShadowAttenuation)
{
    // Let's try to make realtime shadows work on a surface, which already contains
    // baked lighting and shadowing from the main sun light.
    // Summary:
    // 1) Calculate possible value in the shadow by subtracting estimated light contribution from the places occluded by realtime shadow:
    //      a) preserves other baked lights and light bounces
    //      b) eliminates shadows on the geometry facing away from the light
    // 2) Clamp against user defined ShadowColor.
    // 3) Pick original lightmap value, if it is the darkest one.


    // 1) Gives good estimate of illumination as if light would've been shadowed during the bake.
    // We only subtract the main direction light. This is accounted in the contribution term below.
    const half shadowStrength = GetMainLightShadowStrength();
    const float3 lambert = GetMainLight().color * mainLightDiffuseRamp;
    const float3 estimatedLightContributionMaskedByInverseOfShadow = lambert * (1 - mainLightShadowAttenuation);
    const float3 subtractedLightmap = bakedGi - estimatedLightContributionMaskedByInverseOfShadow;

    // 2) Allows user to define overall ambient of the scene and control situation when realtime shadow becomes too dark.
    float3 realtimeShadow = max(subtractedLightmap, _SubtractiveShadowColor.xyz);
    realtimeShadow = lerp(bakedGi, realtimeShadow, shadowStrength);

    // 3) Pick darkest color
    return min(bakedGi, realtimeShadow);
}

void MixRealtimeAndBakedGi(inout float3 inoutBakedGi, const float mainLightDiffuseRamp, const float mainLightShadowAttenuation)
{
    #if defined(LIGHTMAP_ON) && defined(_MIXED_LIGHTING_SUBTRACTIVE)
    inoutBakedGi = SubtractDirectMainLightFromLightmap(inoutBakedGi, mainLightDiffuseRamp, mainLightShadowAttenuation);
    #endif
}

half MixRealtimeAndBakedShadows(half realtimeShadow, half bakedShadow, half shadowFade)
{
    #if defined(LIGHTMAP_SHADOW_MIXING)
    return min(lerp(realtimeShadow, 1, shadowFade), bakedShadow);
    #else // !LIGHTMAP_SHADOW_MIXING
    return lerp(realtimeShadow, bakedShadow, shadowFade);
    #endif // LIGHTMAP_SHADOW_MIXING
}

half BakedShadow(const half4 shadowMask, const half4 occlusionProbeChannels)
{
    // Here occlusionProbeChannels used as mask selector to select shadows in shadowMask
    // If occlusionProbeChannels all components are zero we use default baked shadow value 1.0
    // This code is optimized for mobile platforms:
    // half bakedShadow = any(occlusionProbeChannels) ? dot(shadowMask, occlusionProbeChannels) : 1.0h;
    return half(1.0) + dot(shadowMask - half(1.0), occlusionProbeChannels);
}

Light GetMainLight(const float3 shadowCoords, const float3 positionWs, const float4 shadowMask)
{
    Light light = GetMainLight(shadowCoords, positionWs);
    #ifdef CALCULATE_BAKED_SHADOWS
    half bakedShadow = BakedShadow(shadowMask, _DirectionalLightOcclusionProbes);
    #else // !CALCULATE_BAKED_SHADOWS
    half bakedShadow = half(1.0);
    #endif // CALCULATE_BAKED_SHADOWS

    #ifdef _TOON_RP_ANY_SHADOWS
    half shadowFade = ComputeShadowDistanceFade(positionWs);
    #else // !_TOON_RP_ANY_SHADOWS
    half shadowFade = half(1.0);
    #endif // _TOON_RP_ANY_SHADOWS

    light.shadowAttenuation = MixRealtimeAndBakedShadows(light.shadowAttenuation, bakedShadow, shadowFade);
    return light;
}

#endif // TOON_RP_LIGHTING