#ifndef TOON_RP_VERTEX_COLOR_PAINT_FORWARD_PASS
#define TOON_RP_VERTEX_COLOR_PAINT_FORWARD_PASS

#include "ToonRPVertexColorPaintInput.hlsl"

#include "../../ShaderLibrary/Common.hlsl"
#include "../../ShaderLibrary/DepthNormals.hlsl"
#include "../../ShaderLibrary/Fog.hlsl"

struct appdata
{
    float3 vertex : POSITION;
    float3 normal : NORMAL;
    float4 color : COLOR;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float3 normalWs : NORMAL_WS;
    float3 color : COLOR;

    TOON_RP_FOG_FACTOR_INTERPOLANT

    float4 positionCs : SV_POSITION;
};

v2f VS(const appdata IN)
{
    v2f OUT;

    UNITY_SETUP_INSTANCE_ID(IN);

    const float3 normalWs = TransformObjectToWorldNormal(IN.normal);
    OUT.normalWs = normalWs;
    #ifdef VIEW_ALPHA
    OUT.color = IN.color.aaa;
    #else // !VIEW_ALPHA
    OUT.color = IN.color.rgb;
    #endif // VIEW_ALPHA

    const float3 positionWs = TransformObjectToWorld(IN.vertex);
    const float4 positionCs = TransformWorldToHClip(positionWs);
    OUT.positionCs = positionCs;

    TOON_RP_FOG_FACTOR_TRANSFER(OUT, positionCs);

    return OUT;
}

float4 PS(const v2f IN) : SV_TARGET
{
    static const float3 light_direction = normalize(float3(1, 1, 1));
    const float nDotL = dot(normalize(IN.normalWs), light_direction);

    float3 color = IN.color;
    const float nDotL01 = nDotL * 0.5 + 0.5;
    color *= lerp(_DiffuseIntensity0, _DiffuseIntensity1, nDotL01);

    return float4(color, 1);
}

#endif // TOON_RP_VERTEX_COLOR_PAINT_FORWARD_PASS