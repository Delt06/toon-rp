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

struct appdata
{
    float3 vertex : POSITION;
    float3 normal : NORMAL_SEMANTIC;
    float2 uv : TEXCOORD0;
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

    const float thickness = max(0, rawThickness);
    const float4 positionCs = TransformWorldToHClip(positionWs + normalWs * thickness);
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