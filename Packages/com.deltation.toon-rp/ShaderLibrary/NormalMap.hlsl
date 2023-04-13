#ifndef TOON_RP_NORMAL_MAP
#define TOON_RP_NORMAL_MAP

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

float3 SampleNormal(const float2 uv, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap))
{
    const float4 pn = SAMPLE_TEXTURE2D(bumpMap, sampler_bumpMap, uv);
    return UnpackNormal(pn);
}

#endif // TOON_RP_NORMAL_MAP