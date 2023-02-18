#ifndef TOON_RP_UNITY_INPUT
#define TOON_RP_UNITY_INPUT

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"


// Block Layout should be respected due to SRP Batcher
CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    real4 unity_WorldTransformParams;

    // Velocity
    float4x4 unity_MatrixPreviousM;
    float4x4 unity_MatrixPreviousMI;
CBUFFER_END

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;

#endif // TOON_RP_UNITY_INPUT