#ifndef TOON_RP_RAMP
#define TOON_RP_RAMP

#include "Math.hlsl"

float2 _ToonRP_GlobalRamp;
float2 _ToonRP_GlobalRampSpecular;
float2 _ToonRP_GlobalRampRim;
TEXTURE2D(_ToonRP_GlobalRampTexture);
SAMPLER(sampler_ToonRP_GlobalRampTexture);

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

float ComputeRampTextured(const float nDotL, TEXTURE2D_PARAM(tex, texSampler))
{
    const float t = nDotL * 0.5 + 0.5;
    return SAMPLE_TEXTURE2D(tex, texSampler, float2(t, 0.5));
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
    #ifdef _TOON_RP_GLOBAL_RAMP_TEXTURE
    return ComputeRampTextured(nDotL, TEXTURE2D_ARGS(_ToonRP_GlobalRampTexture, sampler_ToonRP_GlobalRampTexture));
    #else // !_TOON_RP_GLOBAL_RAMP_TEXTURE
    return ComputeGlobalRamp(nDotL, _ToonRP_GlobalRamp);
    #endif // _TOON_RP_GLOBAL_RAMP_TEXTURE
}

float ComputeGlobalRampSpecular(const float nDotH)
{
    return ComputeRamp(nDotH, _ToonRP_GlobalRampSpecular);
}

float ComputeGlobalRampRim(const float fresnel)
{
    return ComputeRamp(fresnel, _ToonRP_GlobalRampRim);
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