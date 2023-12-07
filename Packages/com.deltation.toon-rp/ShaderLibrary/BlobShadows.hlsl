#ifndef TOON_RP_BLOB_SHADOWS
#define TOON_RP_BLOB_SHADOWS

#include "Textures.hlsl"

TEXTURE2D(_ToonRP_BlobShadowMap);
SAMPLER(sampler_ToonRP_BlobShadowMap);

CBUFFER_START(_ToonRPBlobShadows)
    float4 _ToonRP_BlobShadows_Min_Size;
    float2 _ToonRP_BlobShadows_Offset;
CBUFFER_END

float2 ComputeBlobShadowCoordsRaw(const float2 positionWsXz, const float2 boundsOffset, const float2 boundsInvSize)
{
    return (positionWsXz + boundsOffset) * boundsInvSize;
}

float SampleBlobShadowAttenuation(const float3 positionWs)
{
    const float2 uv = ComputeBlobShadowCoordsRaw(positionWs.xz, _ToonRP_BlobShadows_Min_Size.xy, _ToonRP_BlobShadows_Min_Size.zw);
    return 1.0f - SAMPLE_TEXTURE2D_LOD(_ToonRP_BlobShadowMap, sampler_ToonRP_BlobShadowMap, uv, 0).r;
}

#endif // TOON_RP_BLOB_SHADOWS