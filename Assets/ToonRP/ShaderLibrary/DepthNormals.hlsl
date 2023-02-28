#ifndef TOON_RP_DEPTH_NORMALS
#define TOON_RP_DEPTH_NORMALS

#include "Textures.hlsl"

TEXTURE2D(_ToonRP_DepthTexture);
DECLARE_TEXEL_SIZE(_ToonRP_DepthTexture);

TEXTURE2D(_ToonRP_NormalsTexture);
DECLARE_TEXEL_SIZE(_ToonRP_NormalsTexture);

float2 PackNormal(const float2 normal)
{
    return normal.xy * 0.5 + 0.5;
}

float3 UnpackNormal(const float2 packedNormal)
{
    const float2 normalXy = packedNormal * 2 - 1;
    // reconstruct Z
    const float normalZ = sqrt(1 - normalXy.x * normalXy.x - normalXy.y * normalXy.y);
    return float3(normalXy, normalZ);
}

#endif // TOON_RP_DEPTH_NORMALS