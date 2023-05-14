#ifndef TOON_RP_FXAA_LOW_QUALITY
#define TOON_RP_FXAA_LOW_QUALITY

#include "ToonRPPostProcessingStackCommon.hlsl"

// https://www.geeks3d.com/20110405/fxaa-fast-approximate-anti-aliasing-demo-glsl-opengl-test-radeon-geforce/3/

static const float SpanMax = 8.0;
static const float ReduceMul = 1.0 / 8.0;
static const float ReduceMin = 1.0 / 128.0;

float3 ApplyFxaa(const float2 uv)
{
    // Sample colors
    const float3 colorNw = SampleSource(uv);
    const float3 colorNe = SampleSource(uv, float2(1, 0));
    const float3 colorSw = SampleSource(uv, float2(0, 1));
    const float3 colorSe = SampleSource(uv, float2(1, 1));

    // Compute the luminance of samples
    const float lumaNw = Luminance(colorNw);
    const float lumaNe = Luminance(colorNe);
    const float lumaSw = Luminance(colorSw);
    const float lumaSe = Luminance(colorSe);

    const float lumaMin = min(lumaNw, min(lumaNe, min(lumaSw, lumaSe)));
    const float lumaMax = max(lumaNw, max(lumaNe, max(lumaSw, lumaSe)));

    // Find the direction along which to make the final samples
    float2 dir;
    dir.x = -((lumaNw + lumaNe) - (lumaSw + lumaSe));
    dir.y = ((lumaNw + lumaSw) - (lumaNe + lumaSe));

    const float dirReduce = max(
        (lumaNw + lumaNe + lumaSw + lumaSe) * 0.25f * ReduceMul,
        ReduceMin
    );
    const float rcpDirMin = 1.0 / (min(abs(dir.x), abs(dir.y)) + dirReduce);
    dir = clamp(dir * rcpDirMin, -SpanMax, SpanMax) * _MainTex_TexelSize.xy;

    // Sample along the direction
    float3 colorA = 0.5f * (SampleSource(uv + dir * (1.0f / 3.0f - 0.5f)) + SampleSource(
        uv + dir * (2.0f / 3.0f - 0.5f)));
    float3 colorB = colorA * 0.5f + 0.25f * (
        SampleSource(uv + dir * (-0.5)) + SampleSource(uv + dir * 0.5)
    );
    const float lumaB = Luminance(colorB);
    if (lumaB < lumaMin || lumaB > lumaMax)
    {
        return colorA;
    }

    return colorB;
}

#endif // TOON_RP_FXAA_LOW_QUALITY