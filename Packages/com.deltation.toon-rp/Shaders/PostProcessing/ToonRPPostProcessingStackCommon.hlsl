#ifndef TOON_RP_POST_PROCESSING_STACK_COMMON
#define TOON_RP_POST_PROCESSING_STACK_COMMON

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

#include "../../ShaderLibrary/Common.hlsl"
#include "../../ShaderLibrary/Textures.hlsl"

TEXTURE2D_X(_MainTex);
DECLARE_TEXEL_SIZE(_MainTex);

#define LINEAR_SAMPLER sampler_linear_clamp
SAMPLER(LINEAR_SAMPLER);

CBUFFER_START(UnityPerMaterial)
float _FXAA_FixedContrastThreshold;
float _FXAA_RelativeContrastThreshold;
float _FXAA_SubpixelBlendingFactor;

float _ToneMapping_Exposure;

float2 _Vignette_Center;
float _Vignette_Intensity;
float _Vignette_Roundness;
float _Vignette_Smoothness;
float3 _Vignette_Color;

float _FilmGrain_Intensity;
float _FilmGrain_LuminanceThreshold0;
float _FilmGrain_LuminanceThreshold1;
CBUFFER_END

TEXTURE2D(_FilmGrain_Texture);
SAMPLER(sampler_FilmGrain_Texture);
DECLARE_TEXEL_SIZE(_FilmGrain_Texture);

TEXTURE2D(_LookupTable_Texture);
SAMPLER(sampler_LookupTable_Texture);
DECLARE_TEXEL_SIZE(_LookupTable_Texture);

float3 SampleSource(const float2 uv)
{
    return SAMPLE_TEXTURE2D_X_LOD(_MainTex, LINEAR_SAMPLER, uv, 0).rgb;
}

float3 SampleSource(const float2 uv, const float2 pixelOffset)
{
    const float2 offsetUv = uv + pixelOffset * _MainTex_TexelSize.xy;
    return SAMPLE_TEXTURE2D_X_LOD(_MainTex, LINEAR_SAMPLER, offsetUv, 0).rgb;
}

#endif // TOON_RP_POST_PROCESSING_STACK_COMMON