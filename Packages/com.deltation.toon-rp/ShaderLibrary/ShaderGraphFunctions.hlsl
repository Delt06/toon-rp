#ifndef TOON_SHADER_GRAPH_FUNCTIONS_HLSL
#define TOON_SHADER_GRAPH_FUNCTIONS_HLSL

#include "Packages/com.deltation.toon-rp/ShaderLibrary/Common.hlsl"
#include "Packages/com.deltation.toon-rp/ShaderLibrary/Fog.hlsl"

#define SHADERGRAPH_SAMPLE_SCENE_DEPTH(uv) shadergraph_ToonSampleSceneDepth(uv)
#define SHADERGRAPH_SAMPLE_SCENE_COLOR(uv) shadergraph_ToonSampleSceneColor(uv)
#define SHADERGRAPH_FOG(position, color, density) shadergraph_ToonFog(position, color, density)
#define SHADERGRAPH_AMBIENT_SKY unity_AmbientSky
#define SHADERGRAPH_AMBIENT_EQUATOR unity_AmbientEquator
#define SHADERGRAPH_AMBIENT_GROUND unity_AmbientGround
#define SHADERGRAPH_MAIN_LIGHT_DIRECTION shadergraph_ToonRPMainLightDirection

#if defined(REQUIRE_DEPTH_TEXTURE)
#include "Packages/com.deltation.toon-rp/ShaderLibrary/DepthNormals.hlsl"
#endif

#if defined(REQUIRE_OPAQUE_TEXTURE)
#include "Packages/com.deltation.toon-rp/ShaderLibrary/OpaqueTexture.hlsl"
#endif

float shadergraph_ToonSampleSceneDepth(float2 uv)
{
    #if defined(REQUIRE_DEPTH_TEXTURE)
    return SampleDepthTexture(uv);
    #else
    return 0;
    #endif
}

float3 shadergraph_ToonSampleSceneColor(float2 uv)
{
    #if defined(REQUIRE_OPAQUE_TEXTURE)
    return SampleOpaqueTexture(uv);
    #else
    return 0;
    #endif
}

void shadergraph_ToonFog(float3 positionOS, out float4 color, out float density)
{
    color = unity_FogColor;
    #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
    float viewZ = -TransformWorldToView(TransformObjectToWorld(positionOS)).z;
    float nearZ0ToFarZ = max(viewZ - _ProjectionParams.y, 0);
    // ComputeFogFactorZ0ToFar returns the fog "occlusion" (0 for full fog and 1 for no fog) so this has to be inverted for density.
    density = 1.0f - ComputeFogIntensity(ComputeFogFactorZ0ToFar(nearZ0ToFarZ));
    #else
    density = 0.0f;
    #endif
}

// This function assumes the bitangent flip is encoded in tangentWS.w
float3x3 BuildTangentToWorld(float4 tangentWS, float3 normalWS)
{
    // tangentWS must not be normalized (mikkts requirement)

    // Normalize normalWS vector but keep the renormFactor to apply it to bitangent and tangent
    float3 unnormalizedNormalWS = normalWS;
    float renormFactor = 1.0 / length(unnormalizedNormalWS);

    // bitangent on the fly option in xnormal to reduce vertex shader outputs.
    // this is the mikktspace transformation (must use unnormalized attributes)
    float3x3 tangentToWorld = CreateTangentToWorld(unnormalizedNormalWS, tangentWS.xyz, tangentWS.w > 0.0 ? 1.0 : -1.0);

    // surface gradient based formulation requires a unit length initial normal. We can maintain compliance with mikkts
    // by uniformly scaling all 3 vectors since normalization of the perturbed normal will cancel it.
    tangentToWorld[0] = tangentToWorld[0] * renormFactor;
    tangentToWorld[1] = tangentToWorld[1] * renormFactor;
    tangentToWorld[2] = tangentToWorld[2] * renormFactor; // normalizes the interpolated vertex normal

    return tangentToWorld;
}

float3 GetWorldSpaceNormalizeViewDir(const float3 positionWs)
{
    return normalize(GetWorldSpaceViewDir(positionWs));
}

float3 shadergraph_ToonRPMainLightDirection()
{
    return -GetMainLight().direction;
}

// Always include Shader Graph version
// Always include last to avoid double macros
#include "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl"

#endif // TOON_SHADER_GRAPH_FUNCTIONS_HLSL