#ifndef TOON_RP_DEFAULT_FORWARD_PASS
#define TOON_RP_DEFAULT_FORWARD_PASS

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"
#include "../ShaderLibrary/Ramp.hlsl"

struct appdata
{
    float3 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float3 normalWs : NORMAL_WS;
    float3 positionWs : POSITION_WS;
    float3 positionVs : POSITION_VS;
    float4 positionCs : SV_POSITION;
};

#include "ToonRpDefaultInput.hlsl"

v2f VS(const appdata IN)
{
    v2f OUT;

    OUT.uv = APPLY_TILING_OFFSET(IN.uv, _MainTexture);
    OUT.normalWs = TransformObjectToWorldNormal(IN.normal);

    const float3 positionWs = TransformObjectToWorld(IN.vertex);
    OUT.positionWs = positionWs;
    
    float3 positionVs = TransformWorldToView(positionWs);
    #ifdef UNITY_REVERSED_Z
    positionVs.z *= -1.0f;
    #endif // UNITY_REVERSED_Z
    OUT.positionVs = positionVs;
    
    OUT.positionCs = TransformWorldToHClip(positionWs);

    return OUT;
}

float ComputeNDotH(const float3 viewDirectionWs, const float3 normalWs, const float3 lightDirectionWs)
{
    const float3 halfVector = normalize(viewDirectionWs + lightDirectionWs);
    return dot(normalWs, halfVector);
}

float4 PS(const v2f IN) : SV_TARGET
{
    const float3 shadowCoords = TransformWorldToShadowCoords(IN.positionWs);
    const float3 normalWs = normalize(IN.normalWs);
    const Light light = GetMainLight(shadowCoords);
    const float shadowAttenuation = ComputeShadowRamp(light.shadowAttenuation, IN.positionVs);
    const float nDotL = dot(normalWs, light.direction);
    float diffuseRamp = ComputeGlobalRamp(nDotL);
    diffuseRamp = min(diffuseRamp * shadowAttenuation, shadowAttenuation);
    const float3 albedo = _MainColor.rgb * SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, IN.uv).rgb;
    const float3 mixedShadowColor = MixShadowColor(albedo, _ShadowColor);
    const float3 diffuse = light.color * ApplyRamp(albedo, mixedShadowColor, diffuseRamp);

    const float3 viewDirectionWs = normalize(GetWorldSpaceViewDir(IN.positionWs));
    const float nDotH = ComputeNDotH(viewDirectionWs, normalWs, light.direction);
    const float specularRamp = ComputeGlobalRampSpecular(nDotH);
    const float3 specular = light.color * _SpecularColor * specularRamp;

    const float3 outputColor = diffuse + specular;
    return float4(outputColor, 1.0f);
}

#endif // TOON_RP_DEFAULT_FORWARD_PASS