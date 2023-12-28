#include "Packages/com.deltation.toon-rp/ShaderLibrary/Common.hlsl"
#include "Packages/com.deltation.toon-rp/ShaderLibrary/Fog.hlsl"
#include "Packages/com.deltation.toon-rp/Shaders/Extensions/ToonRPInvertedHullOutlineCommon.hlsl"

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

    #ifdef SHADERGRAPH_PREVIEW
    float3 passColor = 1;
    #else // !SHADERGRAPH_PREVIEW
    float3 passColor = _ToonRpInvertedHullOutline_Color;
    #endif // !SHADERGRAPH_PREVIEW

    float3 albedo = 1.0f;
    albedo.rgb = surfaceDescription.Albedo * passColor;

    const float3 emission = surfaceDescription.Emission;
    float3 outputColor = albedo + emission;

    #if !_FORCE_DISABLE_FOG
    const float fogFactor = unpacked.fogFactorAndVertexLight.x;
    outputColor = MixFog(outputColor.rgb, fogFactor);
    #endif // !_FORCE_DISABLE_FOG

    ApplyCustomFog(outputColor, surfaceDescription);

    return float4(outputColor, 1);
}