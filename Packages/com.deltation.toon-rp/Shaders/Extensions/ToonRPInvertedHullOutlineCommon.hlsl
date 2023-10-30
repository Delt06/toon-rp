#ifndef TOON_RP_INVERTED_HULL_OUTLINE_COMMON
#define TOON_RP_INVERTED_HULL_OUTLINE_COMMON

#include "../../ShaderLibrary/Common.hlsl"
#include "../../ShaderLibrary/Fog.hlsl"
#include "../../ShaderLibrary/Math.hlsl"

#if !defined(NORMAL_SEMANTIC)
#define NORMAL_SEMANTIC NORMAL
#endif // !NORMAL_SEMANTIC

#if defined(_VERTEX_COLOR_THICKNESS_R) || defined(_VERTEX_COLOR_THICKNESS_G) || defined(_VERTEX_COLOR_THICKNESS_B) || defined(_VERTEX_COLOR_THICKNESS_A)
#define USE_VERTEX_COLOR_THICKNESS
#endif // _VERTEX_COLOR_THICKNESS_R || _VERTEX_COLOR_THICKNESS_G || _VERTEX_COLOR_THICKNESS_B || _VERTEX_COLOR_THICKNESS_A

#ifndef EXTRA_APP_DATA
#define EXTRA_APP_DATA
#endif // !EXTRA_APP_DATA

struct appdata
{
    float3 vertex : POSITION;
    float3 normal : NORMAL_SEMANTIC;
    
    #ifdef _NOISE
    float2 uv : TEXCOORD0;
    #endif // _NOISE
    
    #ifdef USE_VERTEX_COLOR_THICKNESS
    float4 color : COLOR;
    #endif // USE_VERTEX_COLOR_THICKNESS

    EXTRA_APP_DATA
};

CBUFFER_START(UnityPerMaterial)
float _Thickness;
float3 _Color;
float2 _DistanceFade;
float _NoiseFrequency;
float _NoiseAmplitude;
CBUFFER_END

float GetVertexColorThickness(const appdata IN)
{
    #if defined(_VERTEX_COLOR_THICKNESS_R)
    return IN.color.r;
    #elif defined(_VERTEX_COLOR_THICKNESS_G)
    return IN.color.g;
    #elif defined(_VERTEX_COLOR_THICKNESS_B)
    return IN.color.b;
    #elif defined(_VERTEX_COLOR_THICKNESS_A)
    return IN.color.a;
    #else
    return 1;
    #endif // !_VERTEX_COLOR_THICKNESS_R && !_VERTEX_COLOR_THICKNESS_G && !_VERTEX_COLOR_THICKNESS_B && !_VERTEX_COLOR_THICKNESS_A 

}

float4 ApplyThicknessInCSAndTransformToHClip(const float4x4 worldToHClipMatrix, const float3 positionWs, const float3 normalWs, const float thickness)
{
    const float4 positionCs = mul(worldToHClipMatrix, float4(positionWs, 1.0));
    const float3 normalCs = normalize(mul((float3x3)worldToHClipMatrix, normalWs).xyz);
    return positionCs + float4(normalCs, 0) * thickness * positionCs.w;
}

float4 ApplyThicknessInWSAndTransformToHClip(const float4x4 worldToHClipMatrix, const float3 positionWs, const float3 normalWs, const float thickness)
{
    return mul(worldToHClipMatrix, float4(positionWs + normalWs * thickness, 1.0));
}

float4 ApplyThicknessAndTransformToHClip(const float4x4 worldToHClipMatrix, const float3 positionWs, const float3 normalWs, const float thickness)
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

float ComputeThickness(const appdata IN, float3 positionWs, float3 normalWs)
{
    float rawThickness = _Thickness;

    #ifdef _NOISE
    const float noise = _NoiseAmplitude * frac(_NoiseFrequency * (IN.uv.x + IN.uv.y));
    rawThickness += noise;
    #endif // _NOISE

    #ifdef _DISTANCE_FADE
    const float depth = GetLinearDepth(positionWs);
    const float distanceFade = 1 - DistanceFade(depth, _DistanceFade.x, _DistanceFade.y);
    rawThickness *= distanceFade;
    #endif // _DISTANCE_FADE

    return max(0, rawThickness) * GetVertexColorThickness(IN);
}

#endif // TOON_RP_INVERTED_HULL_OUTLINE_COMMON