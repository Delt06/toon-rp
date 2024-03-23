#ifndef TOON_RP_DEPTH_NORMALS_SHADER_GRAPH
#define TOON_RP_DEPTH_NORMALS_SHADER_GRAPH

#include "DepthNormals.hlsl"

void SampleSceneNormals_float(const float2 screenUv, out float3 normalsWs)
{
#ifdef SHADERGRAPH_PREVIEW
    normalsWs = float3(0, 0, 1);
#else // !SHADERGRAPH_PREVIEW
    normalsWs = SampleNormalsTexture(screenUv);
#endif // SHADERGRAPH_PREVIEW
}

#endif // TOON_RP_DEPTH_NORMALS_SHADER_GRAPH