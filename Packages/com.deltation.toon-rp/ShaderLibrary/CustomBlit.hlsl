#ifndef TOON_RP_CUSTOM_BLIT
#define TOON_RP_CUSTOM_BLIT

#include "Common.hlsl"

struct appdata
{
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float4 positionCs : SV_POSITION;
    float2 uv : TEXCOORD0;
};

v2f VS(const appdata IN)
{
    v2f OUT;

    float4 positionCs = float4(IN.uv, UNITY_RAW_FAR_CLIP_VALUE, 1);
    positionCs.xy = positionCs.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1

    #ifdef TOON_PRETRANSFORM_TO_DISPLAY_ORIENTATION
    positionCs = ApplyPretransformRotation(positionCs);
    #endif // TOON_PRETRANSFORM_TO_DISPLAY_ORIENTATION
    
    OUT.positionCs = positionCs;
    OUT.uv = IN.uv;

    if (_ProjectionParams.x > 0.0)
    {
        OUT.uv.y = 1 - OUT.uv.y;
    }

    return OUT;
}

#endif // TOON_RP_CUSTOM_BLIT