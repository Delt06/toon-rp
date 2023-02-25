#ifndef TOON_RP_INVERTED_HULL_OUTLINE
#define TOON_RP_INVERTED_HULL_OUTLINE

#include "../../ShaderLibrary/Common.hlsl"

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
};

float _ToonRP_Outline_InvertedHull_Thickness;
float3 _ToonRP_Outline_InvertedHull_Color;

v2f VS(const appdata IN)
{
    v2f OUT;

    const float4 positionCs = TransformObjectToHClip(IN.vertex);
    const float3 normalCs = TransformWorldToHClipDir(TransformObjectToWorldNormal(IN.normal));
    OUT.positionCs = positionCs + float4(normalCs * _ToonRP_Outline_InvertedHull_Thickness, 0);

    return OUT;
}

float4 PS() : SV_TARGET
{
    return float4(_ToonRP_Outline_InvertedHull_Color, 1);
			    
}

#endif // TOON_RP_INVERTED_HULL_OUTLINE