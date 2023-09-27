#ifndef TOON_RP_LIGHTING
#define TOON_RP_LIGHTING

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"

#include "Shadows.hlsl"
#include "UnityInput.hlsl"

#define MAX_ADDITIONAL_LIGHT_COUNT 64
#define MAX_ADDITIONAL_LIGHTS_PER_OBJECT 4

CBUFFER_START(_ToonRPLight)
    float3 _DirectionalLightColor;
    float3 _DirectionalLightDirection;

    uint _AdditionalLightCount;
    float4 _AdditionalLightColors[MAX_ADDITIONAL_LIGHT_COUNT]; // xyz = color, w = range
    float4 _AdditionalLightPositions[MAX_ADDITIONAL_LIGHT_COUNT]; // xyz = position, w = 1/range^2
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

Light GetMainLight(const float3 shadowCoords)
{
    Light light;
    light.color = _DirectionalLightColor;
    light.direction = _DirectionalLightDirection;
    light.shadowAttenuation = SampleShadowAttenuation(shadowCoords);
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

Light GetAdditionalLightGlobal(const uint globalLightIndex, const float3 positionWs)
{
    Light light;
    light.color = _AdditionalLightColors[globalLightIndex].rgb;
    const float4 position = _AdditionalLightPositions[globalLightIndex];
    const float3 offset = position.xyz - positionWs;
    light.direction = normalize(offset);
    light.shadowAttenuation = 1.0f;

    const float distanceSqr = max(dot(offset, offset), 0.00001);
    const float distanceAttenuation = Sq(
        saturate(1.0f - Sq(distanceSqr * position.w))
    );
    light.distanceAttenuation = distanceAttenuation / distanceSqr;

    return light;
}

Light GetAdditionalLight(const uint perObjectIndex, const float3 positionWs)
{
    const uint globalIndex = ToGlobalLightIndex(perObjectIndex);
    return GetAdditionalLightGlobal(globalIndex, positionWs);
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