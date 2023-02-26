#ifndef TOON_RP_COMMON
#define TOON_RP_COMMON

#include "./UnityInput.hlsl"

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection

#define UNITY_PREV_MATRIX_M unity_MatrixPreviousM
#define UNITY_PREV_MATRIX_I_M unity_MatrixPreviousMI

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

// Returns 'true' if the current view performs a perspective projection.
bool IsPerspectiveProjection()
{
    return unity_OrthoParams.w == 0;
}

// Camera ("Eye") position in world space
float3 GetCurrentViewPosition()
{
    return _WorldSpaceCameraPos;
}

float3 GetViewForwardDir()
{
    float4x4 viewMat = GetWorldToViewMatrix();
    return -viewMat[2].xyz;
}

float3 GetWorldSpaceViewDir(const float3 positionWs)
{
    if (IsPerspectiveProjection())
    {
        // Perspective
        return GetCurrentViewPosition() - positionWs;
    }

    // Orthographic
    return -GetViewForwardDir();
}

float GetLinearDepth(const float3 positionWs)
{
    float depth = TransformWorldToView(positionWs).z;
    #ifdef UNITY_REVERSED_Z
    depth *= -1.0f;
    #endif // UNITY_REVERSED_Z
    return depth;
}

#endif // TOON_RP_COMMON