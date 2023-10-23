#ifndef TOON_RP_TEXTURES
#define TOON_RP_TEXTURES

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

#define CONSTRUCT_TILING_OFFSET_NAME(textureName) textureName ## _ST
#define DECLARE_TILING_OFFSET(textureName) float4 CONSTRUCT_TILING_OFFSET_NAME(textureName);

#define CONSTRUCT_TEXEL_NAME(textureName) textureName ## _TexelSize
#define DECLARE_TEXEL_SIZE(textureName) float4 CONSTRUCT_TEXEL_NAME(textureName);

#define APPLY_TILING_OFFSET(uv, textureName) (uv) * (CONSTRUCT_TILING_OFFSET_NAME(textureName).xy) + (CONSTRUCT_TILING_OFFSET_NAME(textureName).zw)

#endif // TOON_RP_TEXTURES