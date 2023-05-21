#ifndef TOON_RP_SHADOWS
#define TOON_RP_SHADOWS

#if defined(_TOON_RP_DIRECTIONAL_SHADOWS) || defined(_TOON_RP_DIRECTIONAL_CASCADED_SHADOWS)
#define _TOON_RP_VSM_SHADOWS
#endif // _TOON_RP_DIRECTIONAL_SHADOWS || _TOON_RP_DIRECTIONAL_CASCADED_SHADOWS

#if defined(_TOON_RP_VSM_SHADOWS) || defined(_TOON_RP_BLOB_SHADOWS)
#define _TOON_RP_ANY_SHADOWS
#endif // _TOON_RP_VSM_SHADOWS || _TOON_RP_BLOB_SHADOWS

#include "Math.hlsl"
#include "Ramp.hlsl"
#include "VSM.hlsl"

#define MAX_DIRECTIONAL_LIGHT_COUNT 1
#define MAX_CASCADE_COUNT 4

TEXTURE2D(_ToonRP_DirectionalShadowAtlas);
#ifdef _TOON_RP_VSM
SAMPLER(sampler_ToonRP_DirectionalShadowAtlas);
#else // !_TOON_RP_VSM
SAMPLER_CMP(sampler_ToonRP_DirectionalShadowAtlas);
#endif //_TOON_RP_VSM

TEXTURE2D(_ToonRP_ShadowPattern);
SAMPLER(sampler_ToonRP_ShadowPattern);

CBUFFER_START(_ToonRpShadows)
float4x4 _ToonRP_DirectionalShadowMatrices_VP[MAX_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
float4x4 _ToonRP_DirectionalShadowMatrices_V[MAX_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
int _ToonRP_CascadeCount;
float4 _ToonRP_CascadeCullingSpheres[MAX_CASCADE_COUNT];
float2 _ToonRP_ShadowRamp;
float2 _ToonRP_ShadowDistanceFade;
float2 _ToonRP_ShadowBias; // x - depth, y - normal
float3 _ToonRP_ShadowPatternScale;
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
    #ifdef _TOON_RP_VSM
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
    #else // !_TOON_RP_VSM
    return SAMPLE_TEXTURE2D_SHADOW(_ToonRP_DirectionalShadowAtlas, sampler_ToonRP_DirectionalShadowAtlas,
                                   shadowCoords).r;
    #endif // _TOON_RP_VSM
}

float SampleShadowPattern(const float3 positionWs)
{
    const float2 uv = EncodePositionToUv(positionWs, _ToonRP_ShadowPatternScale);
    return SAMPLE_TEXTURE2D(_ToonRP_ShadowPattern, sampler_ToonRP_ShadowPattern, uv).r;
}

uint ComputeShadowTileIndex(const float3 positionWs)
{
    #ifdef _TOON_RP_DIRECTIONAL_CASCADED_SHADOWS
    
    int i;
    for (i = 0; i < _ToonRP_CascadeCount; i++)
    {
        float4 sphere = _ToonRP_CascadeCullingSpheres[i];
        const float distanceSqr = DistanceSquared(positionWs, sphere.xyz);
        if (distanceSqr < sphere.w)
        {
            return i;
        }
    }
    return _ToonRP_CascadeCount - 1;

    #else // !_TOON_RP_DIRECTIONAL_CASCADED_SHADOWS

    return 0;

    #endif // _TOON_RP_DIRECTIONAL_CASCADED_SHADOWS
}

float3 TransformWorldToShadowCoords(const float3 positionWs, uint tileIndex, const bool perspectiveProjection = false)
{
    float4 shadowCoords = mul(_ToonRP_DirectionalShadowMatrices_VP[tileIndex], float4(positionWs, 1.0f));
    #ifdef _TOON_RP_VSM
    shadowCoords.z = mul(_ToonRP_DirectionalShadowMatrices_V[tileIndex], float4(positionWs, 1.0f)).z;
    #endif // _TOON_RP_VSM

    if (perspectiveProjection)
    {
        shadowCoords.xyz /= shadowCoords.w;
    }

    #ifdef _TOON_RP_VSM
    shadowCoords.z = PackVsmDepth(shadowCoords.z);

    #ifdef UNITY_REVERSED_Z
    shadowCoords.z *= -1.0f;
    #endif // UNITY_REVERSED_Z

    #endif // _TOON_RP_VSM

    return shadowCoords.xyz;
}

float ComputeShadowRamp(const float shadowAttenuation)
{
    float ramp;
    #ifdef _TOON_RP_SHADOWS_RAMP_CRISP
    ramp = StepAntiAliased(_ToonRP_ShadowRamp.x, shadowAttenuation);
    #else // !_TOON_RP_SHADOWS_RAMP_CRISP
    ramp = ComputeRamp(shadowAttenuation, _ToonRP_ShadowRamp);
    #endif // _TOON_RP_SHADOWS_RAMP_CRISP
    return ramp;
}

float ComputeShadowDistanceFade(const float3 positionWs)
{
    const float distanceCamToPixel2 = DistanceSquared(positionWs, _WorldSpaceCameraPos);
    const float fade = DistanceFade(distanceCamToPixel2, _ToonRP_ShadowDistanceFade.x, _ToonRP_ShadowDistanceFade.y);
    return fade;
}

float ComputeShadowRamp(const float shadowAttenuation, const float3 positionWs)
{
    const float ramp = ComputeShadowRamp(shadowAttenuation);
    const float fade = ComputeShadowDistanceFade(positionWs);
    return lerp(ramp, 1, fade);
}

#endif // TOON_RP_SHADOWS