#include "Packages/com.deltation.toon-rp/ShaderLibrary/Common.hlsl"
#include "Packages/com.deltation.toon-rp/ShaderLibrary/MotionVectors.hlsl"

#include "Packages/com.deltation.toon-rp/ShaderLibrary/ShaderGraphForwardDeclarations.hlsl"

PackedVaryings VS(Attributes input)
{
    // ReSharper disable once CppRedundantCastExpression
    Varyings output = (Varyings) 0;
    output = BuildVaryings(input);

    const float3 positionWs = TransformObjectToWorld(input.positionOS);
    output.positionCsNoJitter = mul(_NonJitteredViewProjMatrix, float4(positionWs, 1));

    const float3 previousPosition = (unity_MotionVectorsParams.x == 1) ? input.positionOld : input.positionOS;
    output.previousPositionCsNoJitter = mul(_PrevViewProjMatrix, mul(UNITY_PREV_MATRIX_M, float4(previousPosition, 1)));

    ApplyMotionVectorZBias(output.positionCS);

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

    return float4(CalcNdcMotionVectorFromCsPositions(unpacked.positionCsNoJitter, unpacked.previousPositionCsNoJitter),
                  0, 0);
}