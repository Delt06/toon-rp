#ifndef TOON_SHADOW_CASTER_HLSL
#define TOON_SHADOW_CASTER_HLSL

#include "Common.hlsl"

float4 _ToonRP_LightDirectionPosition; // xyz - direction/position, w - selector (0 - direction, 1 - position)

float3 GetCastingLightDirection(const float3 positionWS)
{
    return normalize(_ToonRP_LightDirectionPosition.xyz - positionWS * _ToonRP_LightDirectionPosition.w);
}

#endif // TOON_SHADOW_CASTER_HLSL