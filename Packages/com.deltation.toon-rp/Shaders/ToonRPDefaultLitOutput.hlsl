﻿#ifndef TOON_RP_DEFAULT_LIT_OUTPUT
#define TOON_RP_DEFAULT_LIT_OUTPUT

#include "ToonRPDefaultV2f.hlsl"

#include "../ShaderLibrary/ToonLighting.hlsl"

float3 ComputeLitOutputColor(const v2f IN, const float4 albedo)
{
    #ifdef _NORMAL_MAP
    const half3 normalTs = SampleNormal(IN.uv, _NormalMap, sampler_NormalMap);
    half3 normalWs = TransformTangentToWorld(normalTs, half3x3(IN.tangentWs, IN.bitangentWs, IN.normalWs));
    #else // !_NORMAL_MAP
    half3 normalWs = IN.normalWs;
    #endif // _NORMAL_MAP
    normalWs = normalize(normalWs);

    const float3 viewDirectionWs = normalize(GetWorldSpaceViewDir(IN.positionWs));
    LightComputationParameters lightComputationParameters;
    lightComputationParameters.positionWs = IN.positionWs;
    lightComputationParameters.positionCs = IN.positionCs;
    lightComputationParameters.normalWs = normalWs;
    lightComputationParameters.viewDirectionWs = viewDirectionWs;
    lightComputationParameters.perVertexAdditionalLights = PER_VERTEX_ADDITIONAL_LIGHTS(IN);
    lightComputationParameters.globalRampUv = IN.uv;
    lightComputationParameters.albedo = albedo;
    lightComputationParameters.shadowColor = _ShadowColor;
    // ReSharper disable once CppEntityAssignedButNoRead
    float shadowAttenuation;
    const float3 lights = ComputeLights(lightComputationParameters, shadowAttenuation);

    const float fresnel = 1 - saturate(dot(viewDirectionWs, normalWs));
    const float rimRamp = ComputeRampRim(fresnel, IN.uv);
    #ifdef RIM
    const float3 rim = _RimColor * rimRamp;
    #else // !RIM
    const float3 rim = 0;
    #endif // RIM

    #ifdef _FORCE_DISABLE_ENVIRONMENT_LIGHT
    const float3 ambient = 0;
    #else // !_FORCE_DISABLE_ENVIRONMENT_LIGHT
    const float3 ambient = SampleSH(normalWs) * albedo.rgb;
    #endif // _FORCE_DISABLE_ENVIRONMENT_LIGHT

    #ifdef EMISSION
    const float3 emission = _EmissionColor * albedo.a;
    #else // !EMISSION
    const float3 emission = 0;
    #endif // EMISSION

    float3 outputColor = lights + rim + ambient + emission;
    TOON_RP_MATCAP_APPLY_MULTIPLICATIVE(outputColor, IN, _MatcapBlend, _MatcapTint);
    TOON_RP_MATCAP_APPLY_ADDITIVE(outputColor, IN, shadowAttenuation, _MatcapBlend, _MatcapTint);
    return outputColor;
}


#endif // TOON_RP_DEFAULT_LIT_OUTPUT