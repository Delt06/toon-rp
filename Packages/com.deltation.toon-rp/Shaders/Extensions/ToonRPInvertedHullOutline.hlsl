#ifndef TOON_RP_INVERTED_HULL_OUTLINE
#define TOON_RP_INVERTED_HULL_OUTLINE

#include "../../ShaderLibrary/Common.hlsl"
#include "../../ShaderLibrary/Fog.hlsl"
#include "../../ShaderLibrary/Math.hlsl"

// TODO: figure out if this works on all hardware (most likely it doesn't)
// https://forum.unity.com/threads/ignoring-some-triangles-in-a-vertex-shader.170834/
#ifdef _DISTANCE_FADE
//#define USE_CLIP_DISTANCE
#endif // _DISTANCE_FADE

#if !defined(NORMAL_SEMANTIC)
#define NORMAL_SEMANTIC NORMAL
#endif // !NORMAL_SEMANTIC

#if defined(_VERTEX_COLOR_THICKNESS_R) || defined(_VERTEX_COLOR_THICKNESS_G) || defined(_VERTEX_COLOR_THICKNESS_B) || defined(_VERTEX_COLOR_THICKNESS_A)
#define USE_VERTEX_COLOR_THICKNESS
#endif // _VERTEX_COLOR_THICKNESS_R || _VERTEX_COLOR_THICKNESS_G || _VERTEX_COLOR_THICKNESS_B || _VERTEX_COLOR_THICKNESS_A



struct appdata
{
    float3 vertex : POSITION;
    float3 normal : NORMAL_SEMANTIC;
    float2 uv : TEXCOORD0;
    #ifdef USE_VERTEX_COLOR_THICKNESS
    float4 color : COLOR;
    #endif // USE_VERTEX_COLOR_THICKNESS
};

struct v2f
{
    float4 positionCs : SV_POSITION;
    #ifdef USE_CLIP_DISTANCE
    float clipDistance : SV_ClipDistance;
    #endif // USE_CLIP_DISTANCE
    TOON_RP_FOG_FACTOR_INTERPOLANT
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

float4 ApplyThicknessAndTransformCS(const float3 positionWs, const float3 normalWs, const float thickness)
{
    const float4 positionCs = TransformWorldToHClip(positionWs);
    const float3 normalCs = normalize(TransformWorldToHClipDir(normalWs));
    return positionCs + float4(normalCs, 0) * thickness * positionCs.w;
}

float4 ApplyThicknessAndTransformWS(const float3 positionWs, const float3 normalWs, const float thickness)
{
    return TransformWorldToHClip(positionWs + normalWs * thickness);
}

float4 ApplyThicknessAndTransform(const float3 positionWs, const float3 normalWs, const float thickness)
{
    #ifdef _FIXED_SCREEN_SPACE_THICKNESS
    return ApplyThicknessAndTransformCS(positionWs, normalWs, thickness);
    #else // !_FIXED_SCREEN_SPACE_THICKNESS
    return ApplyThicknessAndTransformWS(positionWs, normalWs, thickness);
    #endif // _FIXED_SCREEN_SPACE_THICKNESS
}

v2f VS(const appdata IN)
{
    v2f OUT;

    const float3 positionWs = TransformObjectToWorld(IN.vertex);
    const float3 normalWs = TransformObjectToWorldNormal(IN.normal);
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

    const float thickness = max(0, rawThickness) * GetVertexColorThickness(IN);
    const float4 positionCs = ApplyThicknessAndTransform(positionWs, normalWs, thickness);
    OUT.positionCs = positionCs;

    #ifdef USE_CLIP_DISTANCE
    // 0 - keep, -1 - discard 
    OUT.clipDistance = distanceFade > 0 ? 0 : -1;
    #endif // USE_CLIP_DISTANCE

    TOON_RP_FOG_FACTOR_TRANSFER(OUT, positionCs);

    return OUT;
}

float4 PS(const v2f IN) : SV_TARGET
{
    float3 outputColor = _Color;
    TOON_RP_FOG_MIX(IN, outputColor);
    return float4(outputColor, 1);
}

#endif // TOON_RP_INVERTED_HULL_OUTLINE