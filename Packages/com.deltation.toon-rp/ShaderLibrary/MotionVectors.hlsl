#ifndef TOON_RP_MOTION_VECTORS
#define TOON_RP_MOTION_VECTORS

#include "Common.hlsl"
#include "Textures.hlsl"

TEXTURE2D_X(_ToonRP_MotionVectorsTexture);
SAMPLER(sampler_ToonRP_MotionVectorsTexture);

float _ToonRP_ZeroMotionVectors;

// Use last frame positions from the vertex buffer (required for skinned meshes)
bool UseLastFramePositions()
{
    return unity_MotionVectorsParams.x == 1;      
}

bool RenderZeroMotionVectors()
{
    return _ToonRP_ZeroMotionVectors == 1;
}

// This is required to avoid artifacts ("gaps" in the _MotionVectorTexture) on some platform
void ApplyMotionVectorZBias(inout float4 positionCs)
{
    #if defined(UNITY_REVERSED_Z)
    positionCs.z -= unity_MotionVectorsParams.z * positionCs.w;
    #else
    positionCs.z += unity_MotionVectorsParams.z * positionCs.w;
    #endif
}

float2 CalcNdcMotionVectorFromCsPositions(const float4 positionCs, const float4 previousPositionCs)
{
    // Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
    const bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;
    if (forceNoMotion)
    {
        return float2(0.0, 0.0);
    }

    // Non-uniform raster needs to keep the posNDC values in float to avoid additional conversions
    // since uv remap functions use floats
    const float2 positionNdc = positionCs.xy * rcp(positionCs.w);
    const float2 previousPositionNdc = previousPositionCs.xy * rcp(previousPositionCs.w);

    // Calculate forward velocity
    float2 velocity = positionNdc.xy - previousPositionNdc.xy;
    #if UNITY_UV_STARTS_AT_TOP
    velocity.y = -velocity.y;
    #endif

    // Convert velocity from NDC space (-1..1) to UV 0..1 space
    // Note: It doesn't mean we don't have negative values, we store negative or positive offset in UV space.
    // Note: ((posNDC * 0.5 + 0.5) - (prevPosNDC * 0.5 + 0.5)) = (velocity * 0.5)
    velocity.xy *= 0.5;

    return velocity;
}

float2 SampleMotionVectors(const float2 uv)
{
    return SAMPLE_TEXTURE2D_X_LOD(_ToonRP_MotionVectorsTexture, sampler_ToonRP_MotionVectorsTexture, uv, 0).xy;
}

#endif // TOON_RP_UNITY_INPUT