#ifndef TOON_RP_LIGHTING
#define TOON_RP_LIGHTING

#include "Shadows.hlsl"


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

#endif // TOON_RP_LIGHTING