#ifndef TOON_RP_MAIN_LIGHT_SHADER_GRAPH
#define TOON_RP_MAIN_LIGHT_SHADER_GRAPH

#ifndef SHADERGRAPH_PREVIEW
#include "Lighting.hlsl"
#endif // !SHADERGRAPH_PREVIEW

void GetMainLight_float(
    out float3 color,
    out float3 direction
    )
{
    #ifdef SHADERGRAPH_PREVIEW
    color = float3(1, 1, 1);
    direction = normalize(float3(1, 1, 1));
    #else // !SHADERGRAPH_PREVIEW
    const Light light = GetMainLight();
    color = light.color;
    direction = light.direction;
    #endif // SHADERGRAPH_PREVIEW
}

#endif // TOON_RP_MAIN_LIGHT_SHADER_GRAPH