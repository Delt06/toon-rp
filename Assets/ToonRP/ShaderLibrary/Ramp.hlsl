#ifndef TOON_RP_RAMP
#define TOON_RP_RAMP

#include "Math.hlsl"

float2 _ToonRP_GlobalRamp;
float2 _ToonRP_GlobalRampSpecular;

float ComputeRamp(const float value, const float edge1, const float edge2)
{
    return smoothstep(edge1, edge2, value);
}

float ComputeRamp(const float value, const float2 ramp)
{
    return ComputeRamp(value, ramp.x, ramp.y);
}

float ComputeRampAntiAliased(const float nDotL, const float2 ramp)
{
    return StepAntiAliased(ramp.x, nDotL);
}

float ComputeGlobalRamp(const float nDotL, const float2 ramp)
{
    #ifdef _TOON_RP_GLOBAL_RAMP_CRISP
    return ComputeRampAntiAliased(nDotL, ramp);
    #else // !_TOON_RP_GLOBAL_RAMP_CRISP
    return ComputeRamp(nDotL, ramp);
    #endif // _TOON_RP_GLOBAL_RAMP_CRISP 
}

float ComputeGlobalRampDiffuse(const float nDotL)
{
    return ComputeGlobalRamp(nDotL, _ToonRP_GlobalRamp);
}

float ComputeGlobalRampSpecular(const float nDotH)
{
    return ComputeRamp(nDotH, _ToonRP_GlobalRampSpecular);
}

float3 MixShadowColor(const float3 albedo, const float4 shadowColor)
{
    return lerp(albedo, shadowColor.rgb, shadowColor.a);
}

float3 ApplyRamp(const float3 albedo, const float3 mixedShadowColor, const float ramp)
{
    return lerp(mixedShadowColor, albedo, ramp);
}

#endif // TOON_RP_RAMP