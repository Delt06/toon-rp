#ifndef TOON_RP_DEFAULT_INPUT
#define TOON_RP_DEFAULT_INPUT

#include "../ShaderLibrary/Textures.hlsl"

CBUFFER_START(UnityPerMaterial)

float4 _MainColor;
DECLARE_TILING_OFFSET(_MainTexture)
float4 _ShadowColor;
float3 _SpecularColor;

float _OverrideRamp_Threshold;
float _OverrideRamp_SpecularThreshold;
float _OverrideRamp_Smoothness;
float _OverrideRamp_SpecularSmoothness;

CBUFFER_END

TEXTURE2D(_MainTexture);
SAMPLER(sampler_MainTexture);

float2 ConstructOverrideRampDiffuse()
{
    return float2(_OverrideRamp_Threshold, _OverrideRamp_Threshold + _OverrideRamp_Smoothness);
}

float2 ConstructOverrideRampSpecular()
{
    return float2(_OverrideRamp_SpecularThreshold, _OverrideRamp_SpecularThreshold + _OverrideRamp_SpecularSmoothness);
}

#endif // TOON_RP_DEFAULT_INPUT