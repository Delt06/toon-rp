#ifndef TOON_RP_FAKE_ADDITIONAL_LIGHTS_SHADER_GRAPH
#define TOON_RP_FAKE_ADDITIONAL_LIGHTS_SHADER_GRAPH

#include "FakeAdditionalLights.hlsl"
#include "Ramp.hlsl"

void SampleFakeAdditionalLights_float(
    const float3 positionWs,
    const bool bicubicFiltering,
    const bool applyGlobalRamp,
    const float2 globalRampUv,
    out float3 lights,
    out float attenuation
)
{
    #ifdef SHADERGRAPH_PREVIEW
    lights = 0;
    attenuation = 0;
    #else // !SHADERGRAPH_PREVIEW

    const half2 uv = FakeAdditionalLights_PositionToUV(positionWs.xz);

    float4 sample;
    if (bicubicFiltering)
    {
        sample = FakeAdditionalLights_SampleRawBicubic(uv);
    }
    else
    {
        sample = FakeAdditionalLights_SampleRaw(uv);
    }

    lights = sample.rgb;
    const float distanceAttenuation = sample.a;

    if (applyGlobalRamp)
    {
        lights *= ComputeGlobalRampDiffuse(distanceAttenuation * 2 - 1, globalRampUv);
    }

    const float fade = FakeAdditionalLights_DistanceFade(positionWs) * FakeAdditionalLights_HeightFade(positionWs.y);
    lights *= fade;

    attenuation = distanceAttenuation * fade;

    #endif // SHADERGRAPH_PREVIEW
}

#endif // TOON_RP_FAKE_ADDITIONAL_LIGHTS_SHADER_GRAPH