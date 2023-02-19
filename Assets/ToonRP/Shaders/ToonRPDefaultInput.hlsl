#ifndef TOON_RP_DEFAULT_INPUT
#define TOON_RP_DEFAULT_INPUT

#include "../ShaderLibrary/Textures.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _MainColor;
DECLARE_TILING_OFFSET(_MainTexture)
float4 _ShadowColor;
float3 _SpecularColor;
CBUFFER_END

TEXTURE2D(_MainTexture);
SAMPLER(sampler_MainTexture);

#endif // TOON_RP_DEFAULT_INPUT