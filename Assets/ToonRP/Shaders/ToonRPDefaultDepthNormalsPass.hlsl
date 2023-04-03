#ifndef TOON_RP_DEFAULT_DEPTH_NORMALS_PASS
#define TOON_RP_DEFAULT_DEPTH_NORMALS_PASS

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/DepthNormals.hlsl"

struct appdata
{
    float3 vertex : POSITION;
    float3 normal : NORMAL;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 positionCs : SV_POSITION;
    float3 normalWs : NORMAL_WS;
};

v2f VS(const appdata IN)
{
    v2f OUT;

    UNITY_SETUP_INSTANCE_ID(IN);
    
    OUT.positionCs = TransformObjectToHClip(IN.vertex);
    OUT.normalWs = TransformObjectToWorldNormal(IN.normal);
    
    return OUT;
}

float4 PS(const v2f IN) : SV_TARGET
{
    const float3 normalWs = normalize(IN.normalWs);
    return float4(PackNormal(normalWs), 0);
}

#endif // TOON_RP_DEFAULT_DEPTH_ONLY_PASS