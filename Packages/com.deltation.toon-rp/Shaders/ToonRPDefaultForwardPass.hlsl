#ifndef TOON_RP_DEFAULT_FORWARD_PASS
#define TOON_RP_DEFAULT_FORWARD_PASS

#include "../ShaderLibrary/BlobShadows.hlsl"
#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Fog.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"
#include "../ShaderLibrary/NormalMap.hlsl"
#include "../ShaderLibrary/Ramp.hlsl"
#include "../ShaderLibrary/SSAO.hlsl"

#if defined(TOON_RP_SSAO_ANY)
#define REQUIRE_DEPTH_INTERPOLANT
#endif // TOON_RP_SSAO_ANY

#ifdef UNLIT
#include "ToonRPUnlitInput.hlsl"
#else // !UNLIT
#include "ToonRPDefaultInput.hlsl"
#endif // UNLIT

struct appdata
{
    float3 vertex : POSITION;
    #if !defined(UNLIT)
    half3 normal : NORMAL;
    #endif // !UNLIT
    float2 uv : TEXCOORD0;

    #ifdef REQUIRE_TANGENT_INTERPOLANT
    half4 tangent : TANGENT;
    #endif // REQUIRE_TANGENT_INTERPOLANT

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float2 uv : TEXCOORD0;
    #if !defined(UNLIT)
    half3 normalWs : NORMAL_WS;
    float4 positionWs : POSITION_WS;
    float depth : DEPTH_VS;
    #endif // !UNLIT

    #ifdef REQUIRE_TANGENT_INTERPOLANT
    half3 tangentWs : TANGENT_WS;
    half3 bitangentWs : BITANGENT_WS;
    #endif // REQUIRE_TANGENT_INTERPOLANT

    TOON_RP_FOG_FACTOR_INTERPOLANT

    float4 positionCs : SV_POSITION;
};

v2f VS(const appdata IN)
{
    v2f OUT;

    UNITY_SETUP_INSTANCE_ID(IN);

    OUT.uv = APPLY_TILING_OFFSET(IN.uv, _MainTexture);
    const float3 positionWs = TransformObjectToWorld(IN.vertex);

    #if !defined(UNLIT)

    const half3 normalWs = TransformObjectToWorldNormal(IN.normal);
    OUT.normalWs = normalWs;
    OUT.positionWs = float4(positionWs, 1.0f);

    #ifdef REQUIRE_DEPTH_INTERPOLANT
    OUT.depth = GetLinearDepth(positionWs);
    #else // !REQUIRE_DEPTH_INTERPOLANT
    OUT.depth = 0.0f;
    #endif // REQUIRE_DEPTH_INTERPOLANT

    #endif // !UNLIT

    const float4 positionCs = TransformWorldToHClip(positionWs);
    OUT.positionCs = positionCs;

    #ifdef REQUIRE_TANGENT_INTERPOLANT
    ComputeTangentsWs(IN.tangent, normalWs, OUT.tangentWs, OUT.bitangentWs);
    #endif // REQUIRE_TANGENT_INTERPOLANT

    TOON_RP_FOG_FACTOR_TRANSFER(OUT, positionCs);

    return OUT;
}

#if !defined(UNLIT)

float ComputeNDotH(const float3 viewDirectionWs, const float3 normalWs, const float3 lightDirectionWs)
{
    const float3 halfVector = normalize(viewDirectionWs + lightDirectionWs);
    return dot(normalWs, halfVector);
}

float GetShadowAttenuation(const v2f IN, const Light light)
{
    #if defined(_TOON_RP_ANY_SHADOWS) || defined(_RECEIVE_BLOB_SHADOWS)
    
    const float shadowAttenuation = ComputeShadowRamp(light.shadowAttenuation, IN.positionWs);
    return shadowAttenuation;

    #else // !_TOON_RP_ANY_SHADOWS && !_TOON_RP_BLOB_SHADOWS

    return 1.0f;

    #endif  // _TOON_RP_ANY_SHADOWS || _TOON_RP_BLOB_SHADOWS
}

Light GetMainLight(const v2f IN)
{
    #ifdef _TOON_RP_VSM_SHADOWS
    const uint tileIndex = ComputeShadowTileIndex(IN.positionWs);
    const float3 shadowCoords = TransformWorldToShadowCoords(IN.positionWs, tileIndex);
    Light light = GetMainLight(shadowCoords);
    #else // !_TOON_RP_VSM_SHADOWS
    Light light = GetMainLight();
    #endif // _TOON_RP_VSM_SHADOWS

    #if defined(_TOON_RP_BLOB_SHADOWS) && defined(_RECEIVE_BLOB_SHADOWS)

    const float blobShadowAttenuation = SampleBlobShadowAttenuation(IN.positionWs);
    light.shadowAttenuation = blobShadowAttenuation;

    #endif // _TOON_RP_BLOB_SHADOWS && _RECEIVE_BLOB_SHADOWS

    return light;
}

float ComputeRampDiffuse(const float nDotL, const v2f IN)
{
    #ifdef _OVERRIDE_RAMP

    const float2 ramp = ConstructOverrideRampDiffuse();
    return ComputeRamp(nDotL, ramp);
    
    #else // !_OVERRIDE_RAMP

    return ComputeGlobalRampDiffuse(nDotL, IN.uv);

    #endif // _OVERRIDE_RAMP
}

float ComputeRampSpecular(const float nDotH, const v2f IN)
{
    #ifdef _OVERRIDE_RAMP

    const float2 ramp = ConstructOverrideRampSpecular();
    return ComputeRamp(nDotH, ramp);
    
    #else // !_OVERRIDE_RAMP

    return ComputeGlobalRampSpecular(nDotH, IN.uv);

    #endif // _OVERRIDE_RAMP
}

float ComputeRampRim(const float fresnel, const v2f IN)
{
    #ifdef _OVERRIDE_RAMP

    const float2 ramp = ConstructOverrideRampRim();
    return ComputeRamp(fresnel, ramp);
    
    #else // !_OVERRIDE_RAMP

    return ComputeGlobalRampRim(fresnel, IN.uv);

    #endif // _OVERRIDE_RAMP
}

float3 ComputeLitOutputColor(const v2f IN, const float4 albedo)
{
    #ifdef _NORMAL_MAP
    const half3 normalTs = SampleNormal(IN.uv, _NormalMap, sampler_NormalMap);
    half3 normalWs = TransformTangentToWorld(normalTs, half3x3(IN.tangentWs, IN.bitangentWs, IN.normalWs));
    #else // !_NORMAL_MAP
    half3 normalWs = IN.normalWs;
    #endif // _NORMAL_MAP
    normalWs = normalize(normalWs);

    const float3 mixedShadowColor = MixShadowColor(albedo.rgb, _ShadowColor);
    const Light light = GetMainLight(IN);
    const float nDotL = dot(normalWs, light.direction);
    float diffuseRamp = ComputeRampDiffuse(nDotL, IN);
    float shadowAttenuation = GetShadowAttenuation(IN, light);

    #ifdef TOON_RP_SSAO_ANY
    const float2 screenUv = PositionHClipToScreenUv(IN.positionCs);
    shadowAttenuation *= SampleAmbientOcclusion(screenUv, IN.positionWs, IN.depth);
    #endif // TOON_RP_SSAO_ANY

    diffuseRamp = min(diffuseRamp * shadowAttenuation, shadowAttenuation);
    const float3 diffuse = ApplyRamp(albedo.rgb, mixedShadowColor, diffuseRamp);

    const float3 viewDirectionWs = normalize(GetWorldSpaceViewDir(IN.positionWs));
    const float nDotH = ComputeNDotH(viewDirectionWs, normalWs, light.direction);
    float specularRamp = ComputeRampSpecular(nDotH, IN);
    specularRamp = min(specularRamp * shadowAttenuation, shadowAttenuation);
    const float3 specular = _SpecularColor * specularRamp;

    const float fresnel = 1 - saturate(dot(viewDirectionWs, normalWs));
    const float rimRamp = ComputeRampRim(fresnel, IN);
    const float3 rim = _RimColor * rimRamp;

    const float3 ambient = SampleSH(normalWs) * albedo.rgb;

    const float3 outputColor = light.color * (diffuse + specular) + rim + ambient + _EmissionColor * albedo.a;
    return outputColor;
}

#endif // !UNLIT

float4 PS(const v2f IN) : SV_TARGET
{
    float4 albedo = SampleAlbedo(IN.uv);
    AlphaClip(albedo);

    #ifdef _ALPHAPREMULTIPLY_ON
    albedo.rgb *= albedo.a;
    #endif // _ALPHAPREMULTIPLY_ON

    #ifdef UNLIT
    float3 outputColor = albedo.rgb;
    #else // !UNLIT
    float3 outputColor = ComputeLitOutputColor(IN, albedo);
    #endif // UNLIT

    TOON_RP_FOG_MIX(IN, outputColor);

    return float4(outputColor, albedo.a);
}

#endif // TOON_RP_DEFAULT_FORWARD_PASS