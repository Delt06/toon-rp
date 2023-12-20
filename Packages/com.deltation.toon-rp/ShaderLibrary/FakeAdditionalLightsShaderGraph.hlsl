#ifndef TOON_RP_FAKE_ADDITIONAL_LIGHTS_SHADER_GRAPH
#define TOON_RP_FAKE_ADDITIONAL_LIGHTS_SHADER_GRAPH

#include "FakeAdditionalLights.hlsl"
#include "Ramp.hlsl"

void SampleFakeAdditionalLights_float(
    const float3 positionWs,
    const bool applyGlobalRamp,
    const float2 globalRampUv,
    out float3 lights
)
{
    #ifdef SHADERGRAPH_PREVIEW
    lights = 0;
    #else // !SHADERGRAPH_PREVIEW
    
    const float4 sample = FakeAdditionalLights_SampleRaw(positionWs);
    lights = sample.rgb;

    if (applyGlobalRamp)
    {
        const float distanceAttenuation = sample.a;
        lights *= ComputeGlobalRampDiffuse(distanceAttenuation * 2 - 1, globalRampUv);
    }

    lights *= FakeAdditionalLights_DistanceFade(positionWs);
    lights *= FakeAdditionalLights_HeightFade(positionWs.y);

    #endif // SHADERGRAPH_PREVIEW
}

#endif // TOON_RP_FAKE_ADDITIONAL_LIGHTS_SHADER_GRAPH