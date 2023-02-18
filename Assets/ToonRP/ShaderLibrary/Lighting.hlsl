#ifndef TOON_RP_LIGHTING
#define TOON_RP_LIGHTING

CBUFFER_START(_ToonRPLight)
    float3 _DirectionalLightColor;
    float3 _DirectionalLightDirection;
CBUFFER_END

struct Light
{
    float3 color;
    float3 direction;
};

Light GetMainLight()
{
    Light light;
    light.color = _DirectionalLightColor;
    light.direction = _DirectionalLightDirection;
    return light;
}

#endif // TOON_RP_LIGHTING