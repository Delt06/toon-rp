#ifndef TOON_RP_DECALS_SHADER_GRAPH
#define TOON_RP_DECALS_SHADER_GRAPH

#include "Decals.hlsl"

void ComputeDecalSpaceUv_float(const float sceneRawDepth, const float2 screenUv, out float2 decalSpaceUv, out float3 positionWs, out half clipValue)
{
#ifdef SHADERGRAPH_PREVIEW
    decalSpaceUv = screenUv;
    positionWs = 0.0;
    clipValue = 0.5h;
#else // !SHADERGRAPH_PREVIEW
    half3 clipValue3;
    decalSpaceUv = ComputeDecalSpaceUv(sceneRawDepth, screenUv, positionWs, clipValue3);
    clipValue = min(clipValue3.x, min(clipValue3.y, clipValue3.z));
#endif // SHADERGRAPH_PREVIEW 
}

void ComputeDecalAngleClipValue_float(const float3 sceneNormalsWs, const half angleThreshold, out half clipValue)
{
    #ifdef SHADERGRAPH_PREVIEW
    clipValue = 0.0h;
    #else // !SHADERGRAPH_PREVIEW
    clipValue = ComputeDecalAngleClipValue(sceneNormalsWs, angleThreshold);
    #endif // SHADERGRAPH_PREVIEW 
}

void GetDecalAlphaClip_half(const half clipValue, out half alphaClip)
{
    alphaClip = step(0.0, clipValue);
}

#endif // TOON_RP_DECALS_SHADER_GRAPH