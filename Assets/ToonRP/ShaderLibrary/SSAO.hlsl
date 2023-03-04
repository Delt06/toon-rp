#ifndef TOON_RP_SSAO
#define TOON_RP_SSAO

#include "Textures.hlsl"
#include "Math.hlsl"

TEXTURE2D(_ToonRP_SSAOTexture);
SAMPLER(sampler_ToonRP_SSAOTexture);
float2 _ToonRP_SSAO_Ramp;

float SampleAmbientOcclusionRaw(const float2 screenUv)
{
    return SAMPLE_TEXTURE2D(_ToonRP_SSAOTexture, sampler_ToonRP_SSAOTexture, screenUv).x;
}

float SampleAmbientOcclusion(const float2 screenUv)
{
    #ifdef _TOON_RP_SSAO
    const float aoRaw = SampleAmbientOcclusionRaw(screenUv);
    return ComputeRamp(aoRaw, _ToonRP_SSAO_Ramp);
    #else // !_TOON_RP_SSAO
    return 1;
    #endif // _TOON_RP_SSAO
}

#endif // TOON_RP_SSAO