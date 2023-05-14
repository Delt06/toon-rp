#ifndef TOON_RP_POST_PROCESSING_STACK_COMMON
#define TOON_RP_POST_PROCESSING_STACK_COMMON

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

#include "../../ShaderLibrary/Common.hlsl"
#include "../../ShaderLibrary/Textures.hlsl"

TEXTURE2D(_MainTex);
DECLARE_TEXEL_SIZE(_MainTex);

#define LINEAR_SAMPLER sampler_linear_clamp
SAMPLER(LINEAR_SAMPLER);

CBUFFER_START(UnityPerMaterial)
float _FXAA_FixedContrastThreshold;
float _FXAA_RelativeContrastThreshold;
float _FXAA_SubpixelBlendingFactor;

float _FilmGrain_Intensity;
float _FilmGrain_LuminanceThreshold0;
float _FilmGrain_LuminanceThreshold1;
CBUFFER_END

TEXTURE2D(_FilmGrain_Texture);
SAMPLER(sampler_FilmGrain_Texture);
DECLARE_TEXEL_SIZE(_FilmGrain_Texture);

float3 SampleSource(const float2 uv)
{
    return SAMPLE_TEXTURE2D_LOD(_MainTex, LINEAR_SAMPLER, uv, 0);
}

float3 SampleSource(const float2 uv, const float2 pixelOffset)
{
    const float2 offsetUv = uv + pixelOffset * _MainTex_TexelSize.xy;
    return SAMPLE_TEXTURE2D_LOD(_MainTex, LINEAR_SAMPLER, offsetUv, 0);
}

#endif // TOON_RP_POST_PROCESSING_STACK_COMMON