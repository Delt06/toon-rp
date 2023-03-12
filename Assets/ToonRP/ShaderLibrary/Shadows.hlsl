#ifndef TOON_RP_SHADOWS
#define TOON_RP_SHADOWS

#include "Math.hlsl"
#include "Ramp.hlsl"

static const float ToonRp_Vsm_DepthScale = 0.1f;

float PackVsmDepth(const float depth)
{
    return depth * ToonRp_Vsm_DepthScale;
}

#define MAX_DIRECTIONAL_LIGHT_COUNT 1

TEXTURE2D(_ToonRP_DirectionalShadowAtlas);
SAMPLER(sampler_ToonRP_DirectionalShadowAtlas);

CBUFFER_START(_ToonRpShadows)
float4x4 _ToonRP_DirectionalShadowMatrices_VP[MAX_DIRECTIONAL_LIGHT_COUNT];
float4x4 _ToonRP_DirectionalShadowMatrices_V[MAX_DIRECTIONAL_LIGHT_COUNT];
float2 _ToonRP_ShadowRamp;
float2 _ToonRP_ShadowDistanceFade;
float2 _ToonRP_ShadowBias; // x - depth, y - normal
CBUFFER_END

float3 ApplyShadowBias(float3 positionWs, const float3 normalWs, const float3 lightDirection)
{
    const float invNDotL = 1.0 - saturate(dot(lightDirection, normalWs));
    float scale = invNDotL * _ToonRP_ShadowBias.y;

    // normal bias is negative since we want to apply an inset normal offset
    positionWs = lightDirection * _ToonRP_ShadowBias.xxx + positionWs;
    positionWs = normalWs * scale.xxx + positionWs;
    return positionWs;
}

float SampleShadowAttenuation(const float3 shadowCoords)
{
    const float2 varianceSample = SAMPLE_TEXTURE2D(_ToonRP_DirectionalShadowAtlas,
                                                   sampler_ToonRP_DirectionalShadowAtlas,
                                                   shadowCoords.xy).rg;
    const float variance = varianceSample.y - varianceSample.x * varianceSample.x;
    
    #ifdef UNITY_REVERSED_Z
    const float difference = shadowCoords.z - varianceSample.x;
    #else // !UNITY_REVERSED_Z
    const float difference = varianceSample.x - shadowCoords.z;
    #endif // UNITY_REVERSED_Z
    
    if (difference > 0.00001)
    {
        return smoothstep(0.4, 1.0, variance / (variance + difference * difference));
    }

    return 1.0;
}

float3 TransformWorldToShadowCoords(const float3 positionWs, const bool perspectiveProjection = false)
{
    const uint lightIndex = 0;
    float4 shadowCoords = mul(_ToonRP_DirectionalShadowMatrices_VP[lightIndex], float4(positionWs, 1.0f));
    shadowCoords.z = mul(_ToonRP_DirectionalShadowMatrices_V[lightIndex], float4(positionWs, 1.0f)).z;

    if (perspectiveProjection)
    {
        shadowCoords.xyz /= shadowCoords.w;
    }

    shadowCoords.z = PackVsmDepth(shadowCoords.z);

    #ifdef UNITY_REVERSED_Z
    shadowCoords.z *= -1.0f;
    #endif // UNITY_REVERSED_Z

    return shadowCoords.xyz;
}

float ComputeShadowRamp(const float shadowAttenuation, const float depth)
{
    float ramp;
    #ifdef _TOON_RP_DIRECTIONAL_SHADOWS_RAMP_CRISP
    ramp = StepAntiAliased(_ToonRP_ShadowRamp.x, shadowAttenuation);
    #else // !_TOON_RP_DIRECTIONAL_SHADOWS_RAMP_CRISP
    ramp = ComputeRamp(shadowAttenuation, _ToonRP_ShadowRamp);
    #endif // _TOON_RP_DIRECTIONAL_SHADOWS_RAMP_CRISP
    const float fade = DistanceFade(depth, _ToonRP_ShadowDistanceFade.x, _ToonRP_ShadowDistanceFade.y);
    return max(ramp, fade);
}

#endif // TOON_RP_SHADOWS