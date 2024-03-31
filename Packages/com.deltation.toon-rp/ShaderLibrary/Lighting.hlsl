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
    light.shadowAttenuation = SampleShadowAttenuation(shadowCoords, positionWs);
    light.distanceAttenuation = 1.0f;
    return light;
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
};

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
    light.shadowAttenuation = 1.0f;

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

#endif // TOON_RP_LIGHTING