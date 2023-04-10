#ifndef TOON_RP_PARTICLES_UNLIT_FORWARD_PASS
#define TOON_RP_PARTICLES_UNLIT_FORWARD_PASS

#include "../../ShaderLibrary/Common.hlsl"
#include "../../ShaderLibrary/Fog.hlsl"

#include "ToonRPParticlesUnlitInput.hlsl"

struct appdata
{
    float3 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
    float4 color : COLOR;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float3 normalWs : NORMAL_WS;
    float4 color : COLOR;

    TOON_RP_FOG_FACTOR_INTERPOLANT

    float4 positionCs : SV_POSITION;
};

v2f VS(const appdata IN)
{
    v2f OUT;

    UNITY_SETUP_INSTANCE_ID(IN);

    OUT.uv = APPLY_TILING_OFFSET(IN.uv, _MainTexture);
    const float3 normalWs = TransformObjectToWorldNormal(IN.normal);
    OUT.normalWs = normalWs;
    OUT.color = IN.color;

    const float3 positionWs = TransformObjectToWorld(IN.vertex);
    const float4 positionCs = TransformWorldToHClip(positionWs);
    OUT.positionCs = positionCs;

    TOON_RP_FOG_FACTOR_TRANSFER(OUT, positionCs);

    return OUT;
}

float4 PS(const v2f IN) : SV_TARGET
{
    const float4 albedo = SampleAlbedo(IN.uv) * IN.color;
    float3 outputColor = albedo.rgb;
    TOON_RP_FOG_MIX(IN, outputColor);

    return float4(outputColor, albedo.a);
}

#endif // TOON_RP_PARTICLES_UNLIT_FORWARD_PASS