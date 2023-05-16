#ifndef TOON_RP_VIGNETTE
#define TOON_RP_VIGNETTE

#include "ToonRPPostProcessingStackCommon.hlsl"

float3 ApplyVignette(const float3 previousColor, const float2 uv)
{
    // com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl
    float2 dist = abs(uv - _Vignette_Center) * _Vignette_Intensity;
    dist.x *= _Vignette_Roundness;
    const float vfactor = pow(saturate(1.0 - dot(dist, dist)), _Vignette_Smoothness);
    return previousColor * lerp(_Vignette_Color, 1.0, vfactor);
}

#endif // TOON_RP_VIGNETTE