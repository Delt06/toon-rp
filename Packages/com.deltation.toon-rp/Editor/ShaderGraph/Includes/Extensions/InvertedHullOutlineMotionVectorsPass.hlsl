#include "Packages/com.deltation.toon-rp/ShaderLibrary/Common.hlsl"
#include "Packages/com.deltation.toon-rp/ShaderLibrary/MotionVectors.hlsl"
#include "Packages/com.deltation.toon-rp/Shaders/Extensions/ToonRPInvertedHullOutlineCommon.hlsl"

#include "Packages/com.deltation.toon-rp/ShaderLibrary/ShaderGraphForwardDeclarations.hlsl"

PackedVaryings VS(Attributes input)
{
    // ReSharper disable once CppRedundantCastExpression
    Varyings output = (Varyings) 0;
    VertexDescription vertexDescription;
    float3 positionWs, normalWs;
    output = BuildVaryings(input, vertexDescription, positionWs, normalWs);

    const float2 uv = input.uv0.xy;

    UNITY_BRANCH
    if (!RenderZeroMotionVectors())
    {
        {
            const float thickness = ComputeThickness(uv, positionWs, normalWs);
            output.positionCsNoJitter = ApplyThicknessAndTransformToHClip(_NonJitteredViewProjMatrix, positionWs, normalWs, thickness);    
        }

        {
            const float3 previousPositionOs = UseLastFramePositions() ? input.positionOld : input.positionOS;
            // ReSharper disable once CppLocalVariableMayBeConst
            float3 previousPositionWs = mul(UNITY_PREV_MATRIX_M, float4(previousPositionOs, 1)).xyz;
            const float thickness = ComputeThickness(uv, positionWs, normalWs);
            output.previousPositionCsNoJitter = ApplyThicknessAndTransformToHClip(_PrevViewProjMatrix, previousPositionWs, normalWs, thickness);  
        }
    }
    
    ApplyMotionVectorZBias(output.positionCS);

    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

float4 PS(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    const SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);

    UNITY_BRANCH
    if (RenderZeroMotionVectors())
    {
        return 0;
    }

    return float4(CalcNdcMotionVectorFromCsPositions(unpacked.positionCsNoJitter, unpacked.previousPositionCsNoJitter),
                  0, 0);
}