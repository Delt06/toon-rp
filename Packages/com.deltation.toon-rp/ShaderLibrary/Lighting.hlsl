#ifndef TOON_RP_LIGHTING
#define TOON_RP_LIGHTING

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"

#include "Shadows.hlsl"
#include "UnityInput.hlsl"

CBUFFER_START(_ToonRPLight)
float3 _DirectionalLightColor;
float3 _DirectionalLightDirection;
CBUFFER_END

struct Light
{
    float3 color;
    float3 direction;
    float shadowAttenuation;
};

Light GetMainLight()
{
    Light light;
    light.color = _DirectionalLightColor;
    light.direction = _DirectionalLightDirection;
    light.shadowAttenuation = 1.0f;
    return light;
}

Light GetMainLight(const float3 shadowCoords)
{
    Light light;
    light.color = _DirectionalLightColor;
    light.direction = _DirectionalLightDirection;
    light.shadowAttenuation = SampleShadowAttenuation(shadowCoords);
    return light;
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