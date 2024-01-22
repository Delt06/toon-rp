#ifndef TOON_RP_DEPTH_NORMALS
#define TOON_RP_DEPTH_NORMALS

#include "Textures.hlsl"
#include "UnityInput.hlsl"

TEXTURE2D_X(_ToonRP_DepthTexture);
DECLARE_TEXEL_SIZE(_ToonRP_DepthTexture);

TEXTURE2D_X(_ToonRP_NormalsTexture);
DECLARE_TEXEL_SIZE(_ToonRP_NormalsTexture);

#define DEPTH_NORMALS_SAMPLER sampler_point_clamp_DepthNormalsSampler
SAMPLER(DEPTH_NORMALS_SAMPLER);

float3 PackNormal(const float3 normal)
{
    return normal * 0.5 + 0.5;
}

float3 UnpackNormal(const float3 packedNormal)
{
    return packedNormal * 2 - 1;
}

float SampleDepthTexture(const float2 uv)
{
    return SAMPLE_TEXTURE2D_X_LOD(_ToonRP_DepthTexture, DEPTH_NORMALS_SAMPLER, uv, 0).r;
}

float3 SampleNormalsTexture(const float2 uv)
{
    const float3 packedNormal = SAMPLE_TEXTURE2D_X_LOD(_ToonRP_NormalsTexture, DEPTH_NORMALS_SAMPLER, uv, 0).xyz;
    return normalize(UnpackNormal(packedNormal));
}

#endif // TOON_RP_DEPTH_NORMALS