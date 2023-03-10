#ifndef TOON_RP_DEFAULT_DEPTH_ONLY_PASS
#define TOON_RP_DEFAULT_DEPTH_ONLY_PASS

#include "../ShaderLibrary/Common.hlsl"

struct appdata
{
    float3 vertex : POSITION;
};

struct v2f
{
    float4 positionCs : SV_POSITION;
};

v2f VS(const appdata IN)
{
    v2f OUT;
    OUT.positionCs = TransformObjectToHClip(IN.vertex);
    return OUT;
}

float4 PS() : SV_TARGET
{
    return 0;
}

#endif // TOON_RP_DEFAULT_DEPTH_ONLY_PASS