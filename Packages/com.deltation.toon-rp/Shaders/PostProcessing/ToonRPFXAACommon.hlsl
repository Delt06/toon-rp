#ifndef TOON_RP_FXAA_COMMON
#define TOON_RP_FXAA_COMMON

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

#include "../../ShaderLibrary/Common.hlsl"
#include "../../ShaderLibrary/Textures.hlsl"

TEXTURE2D(_MainTex);
DECLARE_TEXEL_SIZE(_MainTex);

#define LINEAR_SAMPLER sampler_linear_clamp
SAMPLER(LINEAR_SAMPLER);

struct appdata
{
    float3 position : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float4 positionCs : SV_POSITION;
    float2 uv : TEXCOORD0;
};

v2f VS(const appdata IN)
{
    v2f OUT;
    OUT.uv = IN.uv;
    OUT.positionCs = TransformObjectToHClip(IN.position);
    return OUT;
}

float3 SampleSource(const float2 uv)
{
    return SAMPLE_TEXTURE2D_LOD(_MainTex, LINEAR_SAMPLER, uv, 0);
}

float3 SampleSource(const float2 uv, const float2 pixelOffset)
{
    const float2 offsetUv = uv + pixelOffset * _MainTex_TexelSize.xy;
    return SAMPLE_TEXTURE2D_LOD(_MainTex, LINEAR_SAMPLER, offsetUv, 0);
}

#endif // TOON_RP_FXAA_COMMON