#ifndef TOON_RP_SAMPLE_MAIN_LIGHT_SHADOW_ATTENUATION_SHADER_GRAPH
#define TOON_RP_SAMPLE_MAIN_LIGHT_SHADOW_ATTENUATION_SHADER_GRAPH

#ifndef SHADERGRAPH_PREVIEW
#include "ToonLighting.hlsl"
#endif // !SHADERGRAPH_PREVIEW

void GetMainLightShadowAttenuation_float(
    const float3 positionWs,
    const float3 shadowReceivePositionOffset,
    const float occlusion,
    out float shadowAttenuation
)
{
    #ifdef SHADERGRAPH_PREVIEW
    shadowAttenuation = 1;
    #else // !SHADERGRAPH_PREVIEW

    #if defined(_TOON_RP_ANY_SHADOWS)

    const float3 shadowPositionWs = positionWs + shadowReceivePositionOffset;

    #ifdef _TOON_RP_SHADOW_MAPS
    const uint tileIndex = ComputeShadowTileIndex(shadowPositionWs);
    const float3 shadowCoords = TransformWorldToShadowCoords(shadowPositionWs, tileIndex);
    Light light = GetMainLight(shadowCoords, positionWs);
    #else // !_TOON_RP_SHADOW_MAPS
    Light light = GetMainLight();
    #endif // _TOON_RP_SHADOW_MAPS

    #if defined(_TOON_RP_BLOB_SHADOWS)
    const float blobShadowAttenuation = SampleBlobShadowAttenuation(shadowPositionWs);
    light.shadowAttenuation = blobShadowAttenuation;
    #endif // _TOON_RP_BLOB_SHADOWS

    LightComputationParameters parameters = (LightComputationParameters) 0;
    parameters.positionWs = positionWs;
    shadowAttenuation = ApplyShadowRampAndPattern(parameters, light.shadowAttenuation * occlusion);

    #else // !_TOON_RP_ANY_SHADOWS

    shadowAttenuation = 1;

    #endif // _TOON_RP_ANY_SHADOWS

    #endif // SHADERGRAPH_PREVIEW
}

#endif // TOON_RP_SAMPLE_MAIN_LIGHT_SHADOW_ATTENUATION_SHADER_GRAPH