#ifndef TOON_RP_OPAQUE_TEXTURE
#define TOON_RP_OPAQUE_TEXTURE

#include "Textures.hlsl"
#include "UnityInput.hlsl"

TEXTURE2D(_ToonRP_OpaqueTexture);
SAMPLER(sampler_ToonRP_OpaqueTexture);
DECLARE_TEXEL_SIZE(_ToonRP_OpaqueTexture);

float3 SampleOpaqueTexture(const float2 uv)
{
    return SAMPLE_TEXTURE2D_LOD(_ToonRP_OpaqueTexture, sampler_ToonRP_OpaqueTexture, uv, 0).rgb;
}

#endif // TOON_RP_OPAQUE_TEXTURE