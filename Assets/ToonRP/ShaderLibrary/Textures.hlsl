﻿#ifndef TOON_RP_TEXTURES
#define TOON_RP_TEXTURES

#define CONSTRUCT_TILING_OFFSET_NAME(textureName) textureName ## _ST
#define DECLARE_TILING_OFFSET(textureName) float4 CONSTRUCT_TILING_OFFSET_NAME(textureName);
#define APPLY_TILING_OFFSET(uv, textureName) (uv) * (CONSTRUCT_TILING_OFFSET_NAME(textureName).xy) + (CONSTRUCT_TILING_OFFSET_NAME(textureName).zw)

#endif // TOON_RP_TEXTURES