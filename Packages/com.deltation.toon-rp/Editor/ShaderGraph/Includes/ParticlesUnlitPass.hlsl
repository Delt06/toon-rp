#include "Packages/com.deltation.toon-rp/ShaderLibrary/Common.hlsl"
#include "Packages/com.deltation.toon-rp/ShaderLibrary/DepthNormals.hlsl"
#include "Packages/com.deltation.toon-rp/ShaderLibrary/Fog.hlsl"
#include "Packages/com.deltation.toon-rp/ShaderLibrary/Particles.hlsl"

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

// Pre-multiplied alpha helper
#if defined(_ALPHAPREMULTIPLY_ON)
#define ALBEDO_MUL albedo
#else
#define ALBEDO_MUL albedo.a
#endif

void ApplySoftParticles(inout float4 albedo, float4 positionCs, const SurfaceDescription surfaceDescription)
{
    const float2 screenUv = PositionHClipToScreenUv(positionCs);
    const float depth = IsOrthographicCamera() ? OrthographicDepthBufferToLinear(positionCs.z) : positionCs.w;
    float bufferDepth = SampleDepthTexture(screenUv);
    bufferDepth = IsOrthographicCamera()
                      ? OrthographicDepthBufferToLinear(bufferDepth)
                      : LinearEyeDepth(bufferDepth, _ZBufferParams);

    #if _SOFT_PARTICLES
    ALBEDO_MUL *= ComputeSoftParticlesFade(depth, bufferDepth, surfaceDescription.SoftParticlesDistance, surfaceDescription.SoftParticlesRange);
    #endif // _SOFT_PARTICLES
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

    ApplySoftParticles(albedo, unpacked.positionCS, surfaceDescription);

    const float3 emission = surfaceDescription.Emission * albedo.a;
    float3 outputColor = albedo.rgb + emission;

    ApplyCustomFog(outputColor, surfaceDescription);

    #if !_FORCE_DISABLE_FOG
    const float fogFactor = unpacked.fogFactorAndVertexLight.x;
    outputColor = MixFog(outputColor.rgb, fogFactor);
    #endif // !_FORCE_DISABLE_FOG

    return float4(outputColor, albedo.a);
}