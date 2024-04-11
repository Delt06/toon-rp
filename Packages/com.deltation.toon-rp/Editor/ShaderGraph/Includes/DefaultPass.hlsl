#include "Packages/com.deltation.toon-rp/ShaderLibrary/Common.hlsl"
#include "Packages/com.deltation.toon-rp/ShaderLibrary/ToonLighting.hlsl"
#include "Packages/com.deltation.toon-rp/ShaderLibrary/Fog.hlsl"

#include "Packages/com.deltation.toon-rp/ShaderLibrary/ShaderGraphForwardDeclarations.hlsl"

PackedVaryings VS(Attributes input)
{
    // ReSharper disable once CppRedundantCastExpression
    Varyings output = (Varyings) 0;
    VertexDescription vertexDescription;
    float3 positionWs, normalWs;
    output = BuildVaryings(input, vertexDescription, positionWs, normalWs);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

float4 PS(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    const SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);

    float4 albedo = 1.0f;
    albedo.rgb = surfaceDescription.Albedo;

    #if _ALPHATEST_ON
    float alpha = surfaceDescription.Alpha;
    clip(alpha - surfaceDescription.AlphaClipThreshold);
    #elif _SURFACE_TYPE_TRANSPARENT
    float alpha = surfaceDescription.Alpha;
    #else
    float alpha = 1;
    #endif
    albedo.a = alpha;

    #ifdef _ALPHAPREMULTIPLY_ON
    albedo.rgb *= albedo.a;
    #endif // _ALPHAPREMULTIPLY_ON

    const float3 normalWs = GetNormalWsFromVaryings(surfaceDescription, unpacked);
    const float3 positionWs = GetPositionWsFromVaryings(surfaceDescription, unpacked);
    const float3 viewDirectionWs = normalize(GetWorldSpaceViewDir(positionWs));

    LightComputationParameters lightComputationParameters = (LightComputationParameters) 0;
    lightComputationParameters.positionWs = positionWs;
    lightComputationParameters.positionCs = unpacked.positionCS;
    lightComputationParameters.normalWs = normalWs;
    lightComputationParameters.viewDirectionWs = viewDirectionWs;
    lightComputationParameters.albedo = albedo;
    lightComputationParameters.shadowColor = surfaceDescription.ShadowColor;
    lightComputationParameters.diffuseOffset = surfaceDescription.DiffuseOffset;
    lightComputationParameters.mainLightOcclusion = surfaceDescription.MainLightOcclusion;
    lightComputationParameters.shadowReceivePositionOffset = surfaceDescription.ShadowReceivePositionOffset;
    lightComputationParameters.lightmapUv = TOON_RP_GI_FRAGMENT_DATA(unpacked);

    #if _TOON_LIGHTING_SPECULAR
    lightComputationParameters.specularSizeOffset = surfaceDescription.SpecularSizeOffset;
    lightComputationParameters.specularColor = surfaceDescription.SpecularColor;
    #endif // _TOON_LIGHTING_SPECULAR
    
    #if _OVERRIDE_RAMP
    lightComputationParameters.overrideRampDiffuse = ConstructOverrideRamp(surfaceDescription.OverrideRampThreshold, surfaceDescription.OverrideRampSmoothness);
    #else //!_OVERRIDE_RAMP
    lightComputationParameters.globalRampUv = surfaceDescription.GlobalRampUV;
    #endif // _OVERRIDE_RAMP

    #if _OVERRIDE_RAMP && _TOON_LIGHTING_SPECULAR
    lightComputationParameters.overrideRampSpecular = ConstructOverrideRamp(surfaceDescription.OverrideRampSpecularThreshold, surfaceDescription.OverrideRampSpecularSmoothness);
    #endif // _OVERRIDE_RAMP && _TOON_LIGHTING_SPECULAR

    #if _OVERRIDE_RAMP && _RIM
    lightComputationParameters.overrideRampRim = ConstructOverrideRamp(surfaceDescription.OverrideRampRimThreshold, surfaceDescription.OverrideRampRimSmoothness);
    #endif // _OVERRIDE_RAMP && _RIM
    
    #ifdef _TOON_RP_ADDITIONAL_LIGHTS_VERTEX
    lightComputationParameters.perVertexAdditionalLights = unpacked.fogFactorAndVertexLight.yzw;
    #endif // _TOON_RP_ADDITIONAL_LIGHTS_VERTEX
    
    // ReSharper disable once CppEntityAssignedButNoRead
    float shadowAttenuation, mainLightDiffuseRamp;
    const float3 lights = ComputeLights(lightComputationParameters, shadowAttenuation, mainLightDiffuseRamp);

    #if _RIM
    const float fresnel = 1 - saturate(dot(viewDirectionWs, normalWs));
    const float rimRamp = ComputeRampRim(lightComputationParameters, fresnel + surfaceDescription.RimSizeOffset);
    const float3 rim = surfaceDescription.RimColor * rimRamp;
    #else // !_RIM
    const float3 rim = 0;
    #endif // _RIM

    #if _FORCE_DISABLE_ENVIRONMENT_LIGHT
    const float3 ambient = 0;
    #else // !_FORCE_DISABLE_ENVIRONMENT_LIGHT
    float3 bakedGi = ComputeBakedGi(lightComputationParameters.lightmapUv, normalWs);
    MixRealtimeAndBakedGi(bakedGi, mainLightDiffuseRamp, shadowAttenuation);
    const float3 ambient = bakedGi * albedo.rgb;
    #endif // _FORCE_DISABLE_ENVIRONMENT_LIGHT

    float3 emission = surfaceDescription.Emission * albedo.a;
    emission = lerp(emission * shadowAttenuation, emission, surfaceDescription.EmissionShadowBlend);

    float3 outputColor = lights + rim + ambient + emission;

    ApplyCustomFog(outputColor, surfaceDescription);

    #if !_FORCE_DISABLE_FOG
    const float fogFactor = unpacked.fogFactorAndVertexLight.x;
    outputColor = MixFog(outputColor.rgb, fogFactor);
    #endif // !_FORCE_DISABLE_FOG

    return float4(outputColor, albedo.a);
}