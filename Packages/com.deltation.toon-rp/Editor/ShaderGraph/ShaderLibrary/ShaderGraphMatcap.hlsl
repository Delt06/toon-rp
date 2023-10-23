#ifndef TOON_RP_SHADER_GRAPH_MATCAP
#define TOON_RP_SHADER_GRAPH_MATCAP

void shadergraph_ComputeMatcapUV_float(const float3 NormalWS, out float2 Result)
{
    half2 matcapUv = TransformWorldToViewDir(NormalWS).xy;
    matcapUv = matcapUv * 0.5 + 0.5;
    Result = matcapUv;
}

void shadergraph_ApplyMatcap_float(const float3 MatcapSample, const float3 Tint, const float3 OriginalColor,
                                  const float Blend, out float3 Result)
{
    const float3 matcapSample = MatcapSample * Tint;
    Result = lerp(OriginalColor, matcapSample * OriginalColor, Blend);
}

#endif // TOON_RP_SHADER_GRAPH_MATCAP