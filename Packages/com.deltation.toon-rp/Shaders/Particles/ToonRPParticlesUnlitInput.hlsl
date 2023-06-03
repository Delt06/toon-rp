#ifndef TOON_RP_PARTICLES_UNLIT_INPUT
#define TOON_RP_PARTICLES_UNLIT_INPUT

#include "../../ShaderLibrary/Textures.hlsl"

CBUFFER_START(UnityPerMaterial)

float4 _MainColor;
DECLARE_TILING_OFFSET(_MainTexture)
float3 _EmissionColor;

float _AlphaClipThreshold;
float _SoftParticlesDistance;
float _SoftParticlesRange;

CBUFFER_END

TEXTURE2D(_MainTexture);
SAMPLER(sampler_MainTexture);

float4 SampleAlbedo(const float2 uv)
{
    return _MainColor * SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, uv);
}

#include "../../ShaderLibrary/AlphaClipping.hlsl"

#endif // TOON_RP_PARTICLES_UNLIT_INPUT