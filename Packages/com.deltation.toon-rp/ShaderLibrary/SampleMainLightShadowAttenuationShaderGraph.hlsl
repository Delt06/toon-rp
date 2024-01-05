#ifndef TOON_RP_SAMPLE_MAIN_LIGHT_SHADOW_ATTENUATION_SHADER_GRAPH
#define TOON_RP_SAMPLE_MAIN_LIGHT_SHADOW_ATTENUATION_SHADER_GRAPH

#ifndef SHADERGRAPH_PREVIEW
#include "ToonLighting.hlsl"
#endif // !SHADERGRAPH_PREVIEW

void GetMainLightShadowAttenuation_float(
    const float3 positionWs,
    const float3 shadowReceivePositionOffset,
    out float shadowAttenuation
    )
{
    #ifdef SHADERGRAPH_PREVIEW
    shadowAttenuation = 1;
    #else // !SHADERGRAPH_PREVIEW

    #if defined(_TOON_RP_ANY_SHADOWS)

    LightComputationParameters parameters = (LightComputationParameters) 0;
    parameters.positionWs = positionWs;
    parameters.shadowReceivePositionOffset = shadowReceivePositionOffset;
    Light light = GetMainLight(parameters);
    shadowAttenuation = ApplyShadowRampAndPattern(parameters, light.shadowAttenuation);

    #endif // _TOON_RP_ANY_SHADOWS

    shadowAttenuation = 1;
    
    #endif // SHADERGRAPH_PREVIEW
}

#endif // TOON_RP_SAMPLE_MAIN_LIGHT_SHADOW_ATTENUATION_SHADER_GRAPH