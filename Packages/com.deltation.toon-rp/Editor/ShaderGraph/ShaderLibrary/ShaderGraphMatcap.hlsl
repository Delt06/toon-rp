#ifndef TOON_RP_SHADER_GRAPH_MATCAP
#define TOON_RP_SHADER_GRAPH_MATCAP

// half2 ComputeMatcapUv(const float3 normalWs)
// {
//     half2 matcapUv = TransformWorldToViewDir(normalWs).xy;
//     matcapUv = matcapUv * 0.5 + 0.5;
//     return matcapUv;
// }
//
// float3 SampleMatcap(const half2 matcapUv, )
// {
//     return SAMPLE_TEXTURE2D(_MatcapTexture, sampler_MatcapTexture, matcapUv).rgb;
// }
//
// float3 SampleMatcapAdditive(const half2 matcapUv, const float3 tint, const float shadowAttenuation,
//                             const float shadowBlend)
// {
//     const float3 matcapSample = SampleMatcap(matcapUv) * tint;
//     return lerp(matcapSample, matcapSample * shadowAttenuation, shadowBlend);
// }
//
// float3 SampleMatcapMultiplicative(const half2 matcapUv, const float3 tint, const float3 originalColor,
//                                   const float blend)
// {
//     const float3 matcapSample = SampleMatcap(matcapUv) * tint;
//     return lerp(originalColor, matcapSample * originalColor, blend);
// }

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