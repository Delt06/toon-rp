#include "Packages/com.deltation.toon-rp/ShaderLibrary/Common.hlsl"
#include "Packages/com.deltation.toon-rp/ShaderLibrary/MotionVectors.hlsl"

#include "Packages/com.deltation.toon-rp/ShaderLibrary/ShaderGraphForwardDeclarations.hlsl"

PackedVaryings VS(Attributes input)
{
    // ReSharper disable once CppRedundantCastExpression
    Varyings output = (Varyings) 0;
    VertexDescription vertexDescription;
    float3 positionWs, normalWs;
    output = BuildVaryings(input, vertexDescription, positionWs, normalWs);

    UNITY_BRANCH
    if (!RenderZeroMotionVectors())
    {
        output.positionCsNoJitter = mul(_NonJitteredViewProjMatrix, float4(positionWs, 1));

        const float3 previousPosition = UseLastFramePositions() ? input.positionOld : input.positionOS;
        output.previousPositionCsNoJitter = mul(_PrevViewProjMatrix, mul(UNITY_PREV_MATRIX_M, float4(previousPosition, 1)));    
    }

    ApplyMotionVectorZBias(output.positionCS);

    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

float2 PS(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    const SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);

    #if _ALPHATEST_ON
    const float alpha = surfaceDescription.Alpha;
    clip(alpha - surfaceDescription.AlphaClipThreshold);
    #endif

    UNITY_BRANCH
    if (RenderZeroMotionVectors())
    {
        return 0;
    }

    return CalcNdcMotionVectorFromCsPositions(unpacked.positionCsNoJitter, unpacked.previousPositionCsNoJitter);
}