#include "Packages/com.deltation.toon-rp/ShaderLibrary/Common.hlsl"
#include "Packages/com.deltation.toon-rp/ShaderLibrary/DepthNormals.hlsl"

#include "Packages/com.deltation.toon-rp/ShaderLibrary/ShaderGraphForwardDeclarations.hlsl"

PackedVaryings VS(Attributes input)
{
    // ReSharper disable once CppRedundantCastExpression
    Varyings output = (Varyings) 0;
    float3 positionWs, normalWs;
    output = BuildVaryings(input, positionWs, normalWs);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

float4 PS(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);

    const SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);

    #if _ALPHATEST_ON
    const float alpha = surfaceDescription.Alpha;
    clip(alpha - surfaceDescription.AlphaClipThreshold);
    #endif

    const float3 normalWS = GetNormalWsFromVaryings(surfaceDescription, unpacked);

    return float4(PackNormal(normalWS), 0);
}