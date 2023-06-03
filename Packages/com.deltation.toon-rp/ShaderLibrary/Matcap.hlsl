#ifndef TOON_RP_MATCAP
#define TOON_RP_MATCAP

#include "Common.hlsl"

#if defined(_MATCAP_ADDITIVE) || defined(_MATCAP_MULTIPLICATIVE)
#define _MATCAP
#endif // _MATCAP_ADDITIVE || _MATCAP_MULTIPLICATIVE

TEXTURE2D(_MatcapTexture);
SAMPLER(sampler_MatcapTexture);

half2 ComputeMatcapUv(const float3 normalWs)
{
    half2 matcapUv = TransformWorldToViewDir(normalWs).xy;
    matcapUv = matcapUv * 0.5 + 0.5;
    return matcapUv;
}

float3 SampleMatcap(const half2 matcapUv)
{
    return SAMPLE_TEXTURE2D(_MatcapTexture, sampler_MatcapTexture, matcapUv).rgb;
}

float3 SampleMatcapAdditive(const half2 matcapUv, const float3 tint, const float shadowAttenuation,
                            const float shadowBlend)
{
    const float3 matcapSample = SampleMatcap(matcapUv) * tint;
    return lerp(matcapSample, matcapSample * shadowAttenuation, shadowBlend);
}

float3 SampleMatcapMultiplicative(const half2 matcapUv, const float3 tint, const float3 originalColor,
                                  const float blend)
{
    const float3 matcapSample = SampleMatcap(matcapUv) * tint;
    return lerp(originalColor, matcapSample * originalColor, blend);
}

#ifdef _MATCAP

#define TOON_RP_MATCAP_UV_INTERPOLANT half2 matcapUv : MATCAP_UV;
#define TOON_RP_MATCAP_UV_TRANSFER(OUT, normalWs) OUT.matcapUv = ComputeMatcapUv(normalWs);

#else // !_MATCAP

#define TOON_RP_MATCAP_UV_INTERPOLANT
#define TOON_RP_MATCAP_UV_TRANSFER(OUT, normalWs)

#endif // _MATCAP

#ifdef _MATCAP_ADDITIVE
#define TOON_RP_MATCAP_APPLY_ADDITIVE(outColor, IN, shadowAttenuation, shadowBlend, tint) outColor += SampleMatcapAdditive(IN.matcapUv, tint, shadowAttenuation, shadowBlend);
#else // !_MATCAP_ADDITIVE
#define TOON_RP_MATCAP_APPLY_ADDITIVE(outColor, IN, shadowAttenuation, shadowBlend, tint)
#endif // _MATCAP_ADDITIVE

#ifdef _MATCAP_MULTIPLICATIVE
#define TOON_RP_MATCAP_APPLY_MULTIPLICATIVE(outColor, IN, blend, tint) outColor = SampleMatcapMultiplicative(IN.matcapUv, tint, outColor, blend);
#else // !_MATCAP_MULTIPLICATIVE
#define TOON_RP_MATCAP_APPLY_MULTIPLICATIVE(outColor, IN, blend, tint)
#endif // _MATCAP_MULTIPLICATIVE


#endif // TOON_RP_MATCAP