#ifndef TOON_RP_FAKE_ADDITIONAL_LIGHTS_SHADER_GRAPH
#define TOON_RP_FAKE_ADDITIONAL_LIGHTS_SHADER_GRAPH

#include "FakeAdditionalLights.hlsl"

void SampleFakeAdditionalLights_float(
    const float3 positionWs,
    out float3 lights
)
{
    #ifdef SHADERGRAPH_PREVIEW
    lights = 0;
    #else // !SHADERGRAPH_PREVIEW
    lights = FakeAdditionalLights_Sample(positionWs);
    #endif // SHADERGRAPH_PREVIEW
}

#endif // TOON_RP_FAKE_ADDITIONAL_LIGHTS_SHADER_GRAPH