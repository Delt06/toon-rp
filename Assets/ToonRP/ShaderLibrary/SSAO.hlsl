#ifndef TOON_RP_SSAO
#define TOON_RP_SSAO

#if defined(_TOON_RP_SSAO) || defined(_TOON_RP_SSAO_PATTERN)
#define TOON_RP_SSAO_ANY
#endif // _TOON_RP_SSAO || _TOON_RP_SSAO_PATTERN

#include "Textures.hlsl"
#include "Math.hlsl"

TEXTURE2D(_ToonRP_SSAOTexture);
SAMPLER(sampler_ToonRP_SSAOTexture);
float2 _ToonRP_SSAO_Ramp;
float3 _ToonRP_SSAO_Pattern_Scale;
float2 _ToonRP_SSAO_Pattern_Ramp;
float2 _ToonRP_SSAO_Pattern_DistanceFade;

float SampleAmbientOcclusionRaw(const float2 screenUv)
{
    return SAMPLE_TEXTURE2D(_ToonRP_SSAOTexture, sampler_ToonRP_SSAOTexture, screenUv).x;
}

float GetAmbientOcclusionPattern(const float3 positionWs, const float depth)
{
    const float3 scaledPosition = positionWs * _ToonRP_SSAO_Pattern_Scale;
    const float seed = scaledPosition.x + scaledPosition.y + scaledPosition.z;
    const float patternBase = abs(frac(seed) - 0.5) * 2;
    const float pattern = ComputeRamp(patternBase, _ToonRP_SSAO_Pattern_Ramp);
    const float distanceFade = DistanceFade(depth, _ToonRP_SSAO_Pattern_DistanceFade.x,
                                            _ToonRP_SSAO_Pattern_DistanceFade.y);
    return pattern * (1 - distanceFade);
}

float SampleAmbientOcclusion(const float2 screenUv, const float3 positionWs, const float depth)
{
    #ifdef TOON_RP_SSAO_ANY
    const float aoRaw = SampleAmbientOcclusionRaw(screenUv);
    const float ao = ComputeRamp(aoRaw, _ToonRP_SSAO_Ramp);
    
    #ifdef _TOON_RP_SSAO_PATTERN
    const float pattern = GetAmbientOcclusionPattern(positionWs, depth);
    return lerp(ao, 1, pattern);
    #else // !_TOON_RP_SSAO_PATTERN
    return ao;
    #endif // _TOON_RP_SSAO_PATTERN

    #else // !TOON_RP_SSAO_ANY
    return 1;
    #endif // TOON_RP_SSAO_ANY
}

#endif // TOON_RP_SSAO