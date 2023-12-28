#include "Packages/com.deltation.toon-rp/ShaderLibrary/Common.hlsl"

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

struct PsOut
{
    #if defined(_TOON_RP_VSM)
    float2 depth_depthSqr : SV_TARGET;
    #endif // _TOON_RP_VSM
};

PsOut PS(PackedVaryings packedInput)
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);

    const SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);

    #if _ALPHATEST_ON
    const float alpha = surfaceDescription.Alpha;
    clip(alpha - surfaceDescription.AlphaClipThreshold);
    #endif

    PsOut OUT;
    
    #ifdef _TOON_RP_VSM
    OUT.depth_depthSqr = float2(unpacked.vsmDepth, unpacked.vsmDepth * unpacked.vsmDepth);
    #endif // _TOON_RP_VSM
    
    return OUT;
}