#ifndef TOON_RP_RAMP
#define TOON_RP_RAMP

#include "Math.hlsl"
#include "Textures.hlsl"

float2 _ToonRP_GlobalRamp;
float2 _ToonRP_GlobalRampSpecular;
float2 _ToonRP_GlobalRampRim;
TEXTURE2D(_ToonRP_GlobalRampTexture);
SAMPLER(sampler_ToonRP_GlobalRampTexture);

float2 ConstructOverrideRamp(const float threshold, const float smoothness)
{
    // See ComputeRamp below for parameter info for each branch
    const float invSmoothness = 1.0f / smoothness;
    return float2(invSmoothness, -threshold * invSmoothness);
}

float ComputeRamp(const float value, const float2 ramp)
{
    return SlopeOffsetFast(ramp.x, ramp.y, value);
}

float ComputeRampAntiAliased(const float nDotL, const float2 ramp)
{
    return StepAntiAliased(ramp.x, nDotL);
}

float ComputeRampTextured(const float ramp, const float2 uv, TEXTURE2D_PARAM(tex, texSampler))
{
    const float v = uv.x + uv.y;
    return SAMPLE_TEXTURE2D(tex, texSampler, float2(ramp, v)).r;
}

float ComputeGlobalRamp(const float nDotL, const float2 ramp)
{
    #ifdef _TOON_RP_GLOBAL_RAMP_CRISP
    return ComputeRampAntiAliased(nDotL, ramp);
    #else // !_TOON_RP_GLOBAL_RAMP_CRISP
    return ComputeRamp(nDotL, ramp);
    #endif // _TOON_RP_GLOBAL_RAMP_CRISP 
}

float ComputeGlobalRampDiffuse(const float nDotL, const float2 uv)
{
    #ifdef _TOON_RP_GLOBAL_RAMP_TEXTURE
    return ComputeRampTextured(nDotL * 0.5 + 0.5, uv, TEXTURE2D_ARGS(_ToonRP_GlobalRampTexture, sampler_ToonRP_GlobalRampTexture));
    #else // !_TOON_RP_GLOBAL_RAMP_TEXTURE
    return ComputeGlobalRamp(nDotL, _ToonRP_GlobalRamp);
    #endif // _TOON_RP_GLOBAL_RAMP_TEXTURE
}

float ComputeGlobalRampSpecular(const float nDotH, const float2 uv)
{
    #ifdef _TOON_RP_GLOBAL_RAMP_TEXTURE
    return ComputeRampTextured(ComputeRamp(nDotH, _ToonRP_GlobalRampSpecular), uv, TEXTURE2D_ARGS(_ToonRP_GlobalRampTexture, sampler_ToonRP_GlobalRampTexture));
    #else // !_TOON_RP_GLOBAL_RAMP_TEXTURE
    return ComputeRamp(nDotH, _ToonRP_GlobalRampSpecular);
    #endif // _TOON_RP_GLOBAL_RAMP_TEXTURE
}

float ComputeGlobalRampRim(const float fresnel, const float2 uv)
{
    #ifdef _TOON_RP_GLOBAL_RAMP_TEXTURE
    return ComputeRampTextured(ComputeRamp(fresnel, _ToonRP_GlobalRampRim), uv, TEXTURE2D_ARGS(_ToonRP_GlobalRampTexture, sampler_ToonRP_GlobalRampTexture));
    #else // !_TOON_RP_GLOBAL_RAMP_TEXTURE
    return ComputeRamp(fresnel, _ToonRP_GlobalRampRim);
    #endif // _TOON_RP_GLOBAL_RAMP_TEXTURE
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