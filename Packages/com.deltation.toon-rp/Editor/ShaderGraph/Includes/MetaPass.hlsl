#include "Packages/com.deltation.toon-rp/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/MetaPass.hlsl"

#include "Packages/com.deltation.toon-rp/ShaderLibrary/ShaderGraphForwardDeclarations.hlsl"

#define MetaInput UnityMetaInput
#define MetaFragment UnityMetaFragment

half4 ToonFragmentMeta(Varyings varyings, MetaInput metaInput)
{
    #ifdef EDITOR_VISUALIZATION
    metaInput.VizUV = varyings.VizUV;
    metaInput.LightCoord = varyings.LightCoord;
    #endif

    return UnityMetaFragment(metaInput);
}

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

half4 PS(PackedVaryings packedInput) : SV_Target
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

    #if _ALPHATEST_ON
    const float alpha = surfaceDescription.Alpha;
    clip(alpha - surfaceDescription.AlphaClipThreshold);
    #endif

    float3 emission = surfaceDescription.Emission * albedo.a;

    MetaInput metaInput;
    metaInput.Albedo = albedo.rgb;
    metaInput.Emission = emission;
    
    return ToonFragmentMeta(unpacked, metaInput);
}