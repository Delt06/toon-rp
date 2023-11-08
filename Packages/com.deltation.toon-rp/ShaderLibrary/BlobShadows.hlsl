#ifndef TOON_RP_BLOB_SHADOWS
#define TOON_RP_BLOB_SHADOWS

#include "Textures.hlsl"

TEXTURE2D(_ToonRP_BlobShadowMap);
SAMPLER(sampler_ToonRP_BlobShadowMap);

CBUFFER_START(_ToonRPBlobShadows)
    float4 _ToonRP_BlobShadows_Min_Size;
    float2 _ToonRP_BlobShadowCoordsOffset;
CBUFFER_END

float SampleBlobShadowAttenuation(float3 positionWs)
{
    const float2 shadowsBoundsMin = _ToonRP_BlobShadows_Min_Size.xy;
    const float2 shadowsBoundsSize = _ToonRP_BlobShadows_Min_Size.zw;
    const float2 uv = (positionWs.xz + _ToonRP_BlobShadowCoordsOffset - shadowsBoundsMin) / shadowsBoundsSize;
    return 1.0f - SAMPLE_TEXTURE2D_LOD(_ToonRP_BlobShadowMap, sampler_ToonRP_BlobShadowMap, uv, 0).r;
}

#endif // TOON_RP_BLOB_SHADOWS