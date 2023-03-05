#ifndef TOON_RP_INVERTED_HULL_OUTLINE
#define TOON_RP_INVERTED_HULL_OUTLINE

#include "../../ShaderLibrary/Common.hlsl"
#include "../../ShaderLibrary/Fog.hlsl"
#include "../../ShaderLibrary/Math.hlsl"

// TODO: figure out if this works on all hardware (most likely it doesn't)
// https://forum.unity.com/threads/ignoring-some-triangles-in-a-vertex-shader.170834/
#define USE_CLIP_DISTANCE

struct appdata
{
    float3 vertex : POSITION;
    #ifdef TOON_RP_USE_TEXCOORD2_NORMALS
    float3 normal : TEXCOORD2;
    #else // !TOON_RP_USE_TEXCOORD2_NORMALS
    float3 normal : NORMAL;
    #endif // TOON_RP_USE_TEXCOORD2_NORMALS
};

struct v2f
{
    float4 positionCs : SV_POSITION;
    #ifdef USE_CLIP_DISTANCE
    float clipDistance : SV_ClipDistance;
    #endif // USE_CLIP_DISTANCE
    TOON_RP_FOG_FACTOR_INTERPOLANT
};

CBUFFER_START(_ToonRpInvertedHullOutline)
float _ToonRP_Outline_InvertedHull_Thickness;
float3 _ToonRP_Outline_InvertedHull_Color;
float2 _ToonRP_Outline_DistanceFade;
CBUFFER_END

v2f VS(const appdata IN)
{
    v2f OUT;

    const float3 positionWs = TransformObjectToWorld(IN.vertex);
    const float4 positionCs = TransformWorldToHClip(positionWs);
    const float3 normalCs = TransformWorldToHClipDir(TransformObjectToWorldNormal(IN.normal));
    const float depth = GetLinearDepth(positionWs);
    const float distanceFade = 1 - DistanceFade(depth, _ToonRP_Outline_DistanceFade.x, _ToonRP_Outline_DistanceFade.y);

    #ifdef USE_CLIP_DISTANCE
    // 0 - keep, -1 - discard 
    OUT.clipDistance = distanceFade > 0 ? 0 : -1;
    #endif // USE_CLIP_DISTANCE

    const float thickness = max(0, _ToonRP_Outline_InvertedHull_Thickness) * distanceFade;
    OUT.positionCs = positionCs + float4(normalCs * thickness, 0);

    TOON_RP_FOG_FACTOR_TRANSFER(OUT, positionCs);

    return OUT;
}

float4 PS(const v2f IN) : SV_TARGET
{
    float3 outputColor = _ToonRP_Outline_InvertedHull_Color;
    TOON_RP_FOG_MIX(IN, outputColor);
    return float4(outputColor, 1);
}

#endif // TOON_RP_INVERTED_HULL_OUTLINE