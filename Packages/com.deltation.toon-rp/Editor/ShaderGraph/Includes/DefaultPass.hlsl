#include "Packages/com.deltation.toon-rp/ShaderLibrary/Common.hlsl"
#include "Packages/com.deltation.toon-rp/ShaderLibrary/ToonLighting.hlsl"
#include "Packages/com.deltation.toon-rp/ShaderLibrary/Fog.hlsl"

#include "Packages/com.deltation.toon-rp/ShaderLibrary/ShaderGraphForwardDeclarations.hlsl"

PackedVaryings VS(Attributes input)
{
    // ReSharper disable once CppRedundantCastExpression
    Varyings output = (Varyings) 0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

float4 PS(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);

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

    LightComputationParameters lightComputationParameters;
    lightComputationParameters.positionWs = unpacked.positionWS;
    lightComputationParameters.positionCs = unpacked.positionCS;
    lightComputationParameters.normalWs = normalWs;
    lightComputationParameters.viewDirectionWs = unpacked.viewDirectionWS;
    lightComputationParameters.globalRampUv = surfaceDescription.GlobalRampUV;
    lightComputationParameters.albedo = albedo;
    lightComputationParameters.shadowColor = surfaceDescription.ShadowColor;

    #if _TOON_LIGHTING_SPECULAR
    lightComputationParameters.specularSizeOffset = surfaceDescription.SpecularSizeOffset;
    lightComputationParameters.specularColor = surfaceDescription.SpecularColor;
    #else // !_TOON_LIGHTING_SPECULAR
    lightComputationParameters.specularSizeOffset = 0.0f;
    lightComputationParameters.specularColor = 1.0f;
    #endif // _TOON_LIGHTING_SPECULAR
    
    
    #ifdef _TOON_RP_ADDITIONAL_LIGHTS_VERTEX
    lightComputationParameters.perVertexAdditionalLights = unpacked.fogFactorAndVertexLight.yzw;
    #else // !_TOON_RP_ADDITIONAL_LIGHTS_VERTEX
    lightComputationParameters.perVertexAdditionalLights = 0;
    #endif // _TOON_RP_ADDITIONAL_LIGHTS_VERTEX
    
    // ReSharper disable once CppEntityAssignedButNoRead
    float shadowAttenuation;
    const float3 lights = ComputeLights(lightComputationParameters, shadowAttenuation);

    #if _FORCE_DISABLE_ENVIRONMENT_LIGHT
    const float3 ambient = 0;
    #else // !_FORCE_DISABLE_ENVIRONMENT_LIGHT
    const float3 ambient = SampleSH(normalWs) * albedo.rgb;
    #endif // _FORCE_DISABLE_ENVIRONMENT_LIGHT

    const float3 emission = surfaceDescription.Emission * albedo.a;

    float3 outputColor = lights + ambient + emission;

    #if !_FORCE_DISABLE_FOG
    const float fogFactor = unpacked.fogFactorAndVertexLight.x;
    outputColor = MixFog(outputColor.rgb, fogFactor);
    #endif // !_FORCE_DISABLE_FOG 

    return float4(outputColor, albedo.a);
}