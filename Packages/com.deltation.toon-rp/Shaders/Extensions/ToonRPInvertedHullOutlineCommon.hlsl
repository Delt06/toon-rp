#ifndef TOON_RP_INVERTED_HULL_OUTLINE_COMMON
#define TOON_RP_INVERTED_HULL_OUTLINE_COMMON

#include "../../ShaderLibrary/Common.hlsl"
#include "../../ShaderLibrary/Fog.hlsl"
#include "../../ShaderLibrary/Math.hlsl"

CBUFFER_START(ToonRpInvertedHullOutline)
    float _ToonRpInvertedHullOutline_Thickness;
    float3 _ToonRpInvertedHullOutline_Color;
    float2 _ToonRpInvertedHullOutline_DistanceFade;
    float _ToonRpInvertedHullOutline_NoiseFrequency;
    float _ToonRpInvertedHullOutline_NoiseAmplitude;
CBUFFER_END

float4 ApplyThicknessInCSAndTransformToHClip(const float4x4 worldToHClipMatrix, const float3 positionWs,
                                             const float3 normalWs, const float thickness)
{
    const float4 positionCs = mul(worldToHClipMatrix, float4(positionWs, 1.0));
    float3 normalCs = normalize(mul((float3x3)worldToHClipMatrix, normalWs).xyz);
    // Apply aspect ratio correction
    normalCs.x *= _ToonRP_ScreenParams.x * _ToonRP_ScreenParams.w;
    return positionCs + float4(normalCs, 0) * thickness * positionCs.w;
}

float4 ApplyThicknessInWSAndTransformToHClip(const float4x4 worldToHClipMatrix, const float3 positionWs,
                                             const float3 normalWs, const float thickness)
{
    return mul(worldToHClipMatrix, float4(positionWs + normalWs * thickness, 1.0));
}

float4 ApplyThicknessAndTransformToHClip(const float4x4 worldToHClipMatrix, const float3 positionWs,
                                         const float3 normalWs, const float thickness)
{
    #ifdef _FIXED_SCREEN_SPACE_THICKNESS
    return ApplyThicknessInCSAndTransformToHClip(worldToHClipMatrix, positionWs, normalWs, thickness);
    #else // !_FIXED_SCREEN_SPACE_THICKNESS
    return ApplyThicknessInWSAndTransformToHClip(worldToHClipMatrix, positionWs, normalWs, thickness);
    #endif // _FIXED_SCREEN_SPACE_THICKNESS
}

float4 ApplyThicknessAndTransformToHClip(const float3 positionWs, const float3 normalWs, const float thickness)
{
    return ApplyThicknessAndTransformToHClip(GetWorldToHClipMatrix(), positionWs, normalWs, thickness);
}

float ComputeThickness(const float2 uv, const float3 positionWs, const float3 normalWs)
{
    float rawThickness = _ToonRpInvertedHullOutline_Thickness;

    #ifdef _NOISE
    const float noise = _ToonRpInvertedHullOutline_NoiseAmplitude * frac(_ToonRpInvertedHullOutline_NoiseFrequency * (uv.x + uv.y));
    rawThickness += noise;
    #endif // _NOISE

    #ifdef _DISTANCE_FADE
    const float depth = GetLinearDepth(positionWs);
    const float2 distanceFadeVec = _ToonRpInvertedHullOutline_DistanceFade;
    const float distanceFade = 1 - DistanceFade(depth, distanceFadeVec.x, distanceFadeVec.y);
    rawThickness *= distanceFade;
    #endif // _DISTANCE_FADE

    return max(0, rawThickness);
}

#endif // TOON_RP_INVERTED_HULL_OUTLINE_COMMON