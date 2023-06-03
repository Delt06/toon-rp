#ifndef TOON_RP_UNITY_INPUT
#define TOON_RP_UNITY_INPUT

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"


// Block Layout should be respected due to SRP Batcher
CBUFFER_START(UnityPerDraw)
// Space block Feature
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4 unity_LODFade; // x is the fade value ranging within [0,1]. y is x quantized into 16 levels
real4 unity_WorldTransformParams; // w is usually 1.0, or -1.0 for odd-negative scale transforms

// Light Indices block feature
real4 unity_LightData;
real4 unity_LightIndices[2];

// Velocity
float4x4 unity_MatrixPreviousM;
float4x4 unity_MatrixPreviousMI;

// SH block feature
real4 unity_SHAr;
real4 unity_SHAg;
real4 unity_SHAb;
real4 unity_SHBr;
real4 unity_SHBg;
real4 unity_SHBb;
real4 unity_SHC;

CBUFFER_END

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 unity_MatrixInvV;
float4x4 unity_MatrixInvP;
float4x4 glstate_matrix_projection;

float3 _WorldSpaceCameraPos;
float4 _ZBufferParams;

// x = orthographic camera's width
// y = orthographic camera's height
// z = unused
// w = 1.0 if camera is ortho, 0.0 if perspective
float4 unity_OrthoParams;

float4 unity_FogParams;
real4 unity_FogColor;

#endif // TOON_RP_UNITY_INPUT