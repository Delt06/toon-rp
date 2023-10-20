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

    // SH block feature
    real4 unity_SHAr;
    real4 unity_SHAg;
    real4 unity_SHAb;
    real4 unity_SHBr;
    real4 unity_SHBg;
    real4 unity_SHBb;
    real4 unity_SHC;

    // Motion Vectors
    float4x4 unity_MatrixPreviousM;
    float4x4 unity_MatrixPreviousMI;
    //X : Use last frame positions (right now skinned meshes are the only objects that use this
    //Y : Force No Motion
    //Z : Z bias value
    float4 unity_MotionVectorsParams;
CBUFFER_END

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 unity_MatrixInvV;
float4x4 unity_MatrixInvP;
float4x4 glstate_matrix_projection;

// Time (t = time since current level load) values from Unity
float4 _Time; // (t/20, t, t*2, t*3)
float4 _SinTime; // sin(t/8), sin(t/4), sin(t/2), sin(t)
float4 _CosTime; // cos(t/8), cos(t/4), cos(t/2), cos(t)
float4 unity_DeltaTime; // dt, 1/dt, smoothdt, 1/smoothdt
float4 _TimeParameters; // t, sin(t), cos(t)

float3 _WorldSpaceCameraPos;
float4 _ZBufferParams;

// x = orthographic camera's width
// y = orthographic camera's height
// z = unused
// w = 1.0 if camera is ortho, 0.0 if perspective
float4 unity_OrthoParams;

real4 unity_AmbientSky;
real4 unity_AmbientEquator;
real4 unity_AmbientGround;
float4 unity_FogParams;
real4 unity_FogColor;

float4x4 _PrevViewProjMatrix; // non-jittered. Motion vectors.
float4x4 _NonJitteredViewProjMatrix; // non-jittered.

bool IsOrthographicCamera()
{
    return unity_OrthoParams.w == 1.0;
}

#endif // TOON_RP_UNITY_INPUT