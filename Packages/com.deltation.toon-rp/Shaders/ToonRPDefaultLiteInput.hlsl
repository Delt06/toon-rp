#ifndef TOON_RP_DEFAULT_LITE_INPUT
#define TOON_RP_DEFAULT_LITE_INPUT

#include "../ShaderLibrary/Textures.hlsl"

CBUFFER_START(UnityPerMaterial)

float4 _MainColor;
DECLARE_TILING_OFFSET(_MainTexture)
float4 _ShadowColor;

float _AlphaClipThreshold;

float _OverrideRamp_Threshold;
float _OverrideRamp_SpecularThreshold;
float _OverrideRamp_RimThreshold;
float _OverrideRamp_Smoothness;
float _OverrideRamp_SpecularSmoothness;
float _OverrideRamp_RimSmoothness;

float3 _MatcapTint;
float _MatcapBlend;

CBUFFER_END

TEXTURE2D(_MainTexture);
SAMPLER(sampler_MainTexture);

TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);

float2 ConstructOverrideRampDiffuse()
{
    return float2(_OverrideRamp_Threshold, _OverrideRamp_Threshold + _OverrideRamp_Smoothness);
}

float2 ConstructOverrideRampSpecular()
{
    return float2(_OverrideRamp_SpecularThreshold, _OverrideRamp_SpecularThreshold + _OverrideRamp_SpecularSmoothness);
}

float2 ConstructOverrideRampRim()
{
    return float2(_OverrideRamp_RimThreshold, _OverrideRamp_RimThreshold + _OverrideRamp_RimSmoothness);
}

float4 SampleAlbedo(const float2 uv)
{
    return _MainColor * SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, uv);
}

#include "../ShaderLibrary/AlphaClipping.hlsl"

#endif // TOON_RP_DEFAULT_LITE_INPUT