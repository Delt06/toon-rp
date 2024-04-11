#ifndef TOON_RP_UNITY_INPUT
#define TOON_RP_UNITY_INPUT

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

#if defined(STEREO_INSTANCING_ON) && (defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_PSSL) || defined(SHADER_API_VULKAN) || (defined(SHADER_API_METAL) && !defined(UNITY_COMPILER_DXC)))
#define UNITY_STEREO_INSTANCING_ENABLED
#endif

#if defined(STEREO_MULTIVIEW_ON) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_VULKAN)) && !(defined(SHADER_API_SWITCH))
    #define UNITY_STEREO_MULTIVIEW_ENABLED
#endif

#if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
#define USING_STEREO_MATRICES
#endif

#ifndef UNITY_SHADER_VARIABLES_INCLUDED

#if defined(USING_STEREO_MATRICES)
// Current pass transforms.
#define glstate_matrix_projection     unity_StereoMatrixP[unity_StereoEyeIndex] // goes through GL.GetGPUProjectionMatrix()
#define unity_MatrixV                 unity_StereoMatrixV[unity_StereoEyeIndex]
#define unity_MatrixInvV              unity_StereoMatrixInvV[unity_StereoEyeIndex]
#define unity_MatrixInvP              unity_StereoMatrixInvP[unity_StereoEyeIndex]
#define unity_MatrixInvVP              unity_StereoMatrixInvVP[unity_StereoEyeIndex]
#define unity_MatrixVP                unity_StereoMatrixVP[unity_StereoEyeIndex]

// Camera transform (but the same as pass transform for XR).
#define _WorldSpaceCameraPos          unity_StereoWorldSpaceCameraPos[unity_StereoEyeIndex]
#endif // USING_STEREO_MATRICES

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

    float4 unity_ProbesOcclusion;

    // Lightmap block feature
    float4 unity_LightmapST;
    float4 unity_DynamicLightmapST;

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

#if !defined(USING_STEREO_MATRICES)
float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 unity_MatrixInvV;
float4x4 unity_MatrixInvP;
float4x4 unity_MatrixInvVP;
float4x4 glstate_matrix_projection;
float4 unity_StereoScaleOffset;
int unity_StereoEyeIndex;
#endif // !USING_STEREO_MATRICES

CBUFFER_START(UnityPerFrame)
    // Time (t = time since current level load) values from Unity
    float4 _Time; // (t/20, t, t*2, t*3)
    float4 _SinTime; // sin(t/8), sin(t/4), sin(t/2), sin(t)
    float4 _CosTime; // cos(t/8), cos(t/4), cos(t/2), cos(t)
    float4 unity_DeltaTime; // dt, 1/dt, smoothdt, 1/smoothdt
    float4 _TimeParameters; // t, sin(t), cos(t)

    float4 _ZBufferParams;

    float4 _ScreenParams; // xy = resolution, zw = 1 + 1 / resolution
    float4 _ProjectionParams;

    // x = orthographic camera's width
    // y = orthographic camera's height
    // z = unused
    // w = 1.0 if camera is ortho, 0.0 if perspective
    float4 unity_OrthoParams;

    real4 unity_AmbientSky;
    real4 unity_AmbientEquator;
    real4 unity_AmbientGround;
CBUFFER_END

#if !defined(USING_STEREO_MATRICES)
float3 _WorldSpaceCameraPos;
#endif // !USING_STEREO_MATRICES

#if defined(USING_STEREO_MATRICES)
CBUFFER_START(UnityStereoViewBuffer)
float4x4 unity_StereoMatrixP[2];
float4x4 unity_StereoMatrixInvP[2];
float4x4 unity_StereoMatrixV[2];
float4x4 unity_StereoMatrixInvV[2];
float4x4 unity_StereoMatrixVP[2];
float4x4 unity_StereoMatrixInvVP[2];

float4x4 unity_StereoCameraProjection[2];
float4x4 unity_StereoCameraInvProjection[2];

float3   unity_StereoWorldSpaceCameraPos[2];
float4   unity_StereoScaleOffset[2];
CBUFFER_END
#endif // USING_STEREO_MATRICES

#if defined(UNITY_STEREO_MULTIVIEW_ENABLED) && defined(SHADER_STAGE_VERTEX)
// OVR_multiview
// In order to convey this info over the DX compiler, we wrap it into a cbuffer.
#if !defined(UNITY_DECLARE_MULTIVIEW)
#define UNITY_DECLARE_MULTIVIEW(number_of_views) CBUFFER_START(OVR_multiview) uint gl_ViewID; uint numViews_##number_of_views; CBUFFER_END
#define UNITY_VIEWID gl_ViewID
#endif
#endif

#if defined(UNITY_STEREO_MULTIVIEW_ENABLED) && defined(SHADER_STAGE_VERTEX)
#define unity_StereoEyeIndex UNITY_VIEWID
UNITY_DECLARE_MULTIVIEW(2);
#elif defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
static uint unity_StereoEyeIndex;
#elif defined(UNITY_SINGLE_PASS_STEREO)
CBUFFER_START(UnityStereoEyeIndex)
int unity_StereoEyeIndex;
CBUFFER_END
#endif

CBUFFER_START(UnityFog)
    float4 unity_FogParams;
    real4 unity_FogColor;
CBUFFER_END

CBUFFER_START(ToonRpJitteredMatrices)
    float4x4 _PrevViewProjMatrix; // non-jittered. Motion vectors.
    float4x4 _NonJitteredViewProjMatrix; // non-jittered.
CBUFFER_END

#endif // !UNITY_SHADER_VARIABLES_INCLUDED

bool IsOrthographicCamera()
{
    return unity_OrthoParams.w == 1.0;
}

#endif // TOON_RP_UNITY_INPUT