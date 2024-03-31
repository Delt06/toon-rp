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

float2 PackNormal(const float3 normal)
{
    return PackNormalOctQuadEncode(normal) * 0.5 + 0.5;
}

float3 UnpackNormal(const float2 packedNormal)
{
    return UnpackNormalOctQuadEncode(packedNormal.xy * 2.0 - 1.0);
}

float SampleDepthTexture(const float2 uv)
{
    return SAMPLE_TEXTURE2D_X_LOD(_ToonRP_DepthTexture, DEPTH_NORMALS_SAMPLER, uv, 0).r;
}

float RawToNdcDepth(float depth)
{
    // GL/GLES are the only APIs, which have NDC depth in the range [-1, 1]
    #if defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3)
    depth = depth * 2.0 - 1.0;
    #endif

    return depth;
}

float3 SampleNormalsTexture(const float2 uv)
{
    const float2 packedNormal = SAMPLE_TEXTURE2D_X_LOD(_ToonRP_NormalsTexture, DEPTH_NORMALS_SAMPLER, uv, 0).xy;
    return normalize(UnpackNormal(packedNormal));
}

#endif // TOON_RP_DEPTH_NORMALS