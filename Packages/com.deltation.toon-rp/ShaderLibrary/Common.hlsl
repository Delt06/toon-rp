#ifndef TOON_RP_COMMON
#define TOON_RP_COMMON

#include "./UnityInput.hlsl"

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_I_VP unity_MatrixInvVP
#define UNITY_MATRIX_P glstate_matrix_projection
#define UNITY_MATRIX_I_V   unity_MatrixInvV
#define UNITY_MATRIX_I_P   unity_MatrixInvP

#define UNITY_PREV_MATRIX_M unity_MatrixPreviousM
#define UNITY_PREV_MATRIX_I_M unity_MatrixPreviousMI

// Include order is important here, instancing should come after macro definitions
#include "ToonInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

CBUFFER_START(ToonRpScreenParams)
    float4 _ToonRP_ScreenParams; // xy = 1 / resolution, zw = resolution (viewport)
    float4 _ToonRP_FullScreenParams; // xy = 1 / resolution, zw = resolution (full screen)
    float4 _ToonRP_ViewportRect; // xy = 1 / size, zw = -xy_min / size
CBUFFER_END

#if UNITY_REVERSED_Z
// TODO: workaround. There's a bug where SHADER_API_GL_CORE gets erroneously defined on switch.
#if (defined(SHADER_API_GLCORE) && !defined(SHADER_API_SWITCH)) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
        //GL with reversed z => z clip range is [near, -far] -> remapping to [0, far]
        #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max((coord - _ProjectionParams.y)/(-_ProjectionParams.z-_ProjectionParams.y)*_ProjectionParams.z, 0)
#else
//D3d with reversed Z => z clip range is [near, 0] -> remapping to [0, far]
//max is required to protect ourselves from near plane not being correct/meaningful in case of oblique matrices.
#define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(((1.0-(coord)/_ProjectionParams.y)*_ProjectionParams.z),0)
#endif
#elif UNITY_UV_STARTS_AT_TOP
    //D3d without reversed z => z clip range is [0, far] -> nothing to do
    #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) (coord)
#else
    //Opengl => z clip range is [-near, far] -> remapping to [0, far]
    #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(((coord + _ProjectionParams.y)/(_ProjectionParams.z+_ProjectionParams.y))*_ProjectionParams.z, 0)
#endif


#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

#ifndef UNITY_SHADER_VARIABLES_INCLUDED

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

float3 GetObjectPosition()
{
    return UNITY_MATRIX_M._m03_m13_m23;
}

float3 GetObjectScale()
{
    float3 scale;
    scale.x = length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x));
    scale.y = length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y));
    scale.z = length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z));
    return scale;
}

#endif // !UNITY_SHADER_VARIABLES_INCLUDED

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

#if defined(UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION) && defined(UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_0)
#define TOON_PRETRANSFORM_TO_DISPLAY_ORIENTATION
#endif // UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION && UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_0

#ifdef TOON_PRETRANSFORM_TO_DISPLAY_ORIENTATION
float2 ApplyPretransformRotationPixelCoords2(float2 v)
{
    switch (UNITY_DISPLAY_ORIENTATION_PRETRANSFORM)
    {
    default:
    case UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_0   :                                                   break;
    case UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_90  : v.xy = float2(v.y, _ToonRP_ScreenParams.w - v.x); break;
    case UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_180 : v.xy = _ToonRP_ScreenParams.zw - v.xy;            break;
    case UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_270 : v.xy = float2(_ToonRP_ScreenParams.z - v.y, v.x); break;
    }
    return v;
}

float4 ApplyPretransformRotationPixelCoords(float4 v)
{
    return float4(ApplyPretransformRotationPixelCoords2(v.xy), v.zw);
}
#endif // TOON_PRETRANSFORM_TO_DISPLAY_ORIENTATION

float2 PositionHClipToScreenUv(float4 positionCs, const bool applyViewportRect = true)
{
    #ifdef TOON_PRETRANSFORM_TO_DISPLAY_ORIENTATION
    positionCs = ApplyPretransformRotationPixelCoords(positionCs);
    #endif // TOON_PRETRANSFORM_TO_DISPLAY_ORIENTATION

    float2 screenUv = positionCs.xy * _ToonRP_FullScreenParams.xy;

    #ifdef UNITY_UV_STARTS_AT_TOP
    if (_ProjectionParams.x > 0.0)
    {
        screenUv.y = 1.0f - screenUv.y;
    }
    #endif // UNITY_UV_STARTS_AT_TOP

    if (applyViewportRect)
    {
        // this is equivalent to InverseLerp(viewportRect.min, viewportRect.max, screenUv)
        screenUv = screenUv * _ToonRP_ViewportRect.xy + _ToonRP_ViewportRect.zw;
    }

    return screenUv;
}

void ComputeTangentsWs(const half4 tangentOs, const half3 normalWs, out half3 tangentWs, out half3 bitangentWs)
{
    // mikkts space compliant. only normalize when extracting normal at frag.
    const half sign = tangentOs.w * GetOddNegativeScale();
    tangentWs = TransformObjectToWorldDir(tangentOs.xyz);
    bitangentWs = cross(normalWs, tangentWs) * sign;
}

float OrthographicDepthBufferToLinear(float rawDepth)
{
    #if UNITY_REVERSED_Z
    rawDepth = 1.0 - rawDepth;
    #endif
    return (_ProjectionParams.z - _ProjectionParams.y) * rawDepth + _ProjectionParams.y;
}

struct VertexPositionInputs
{
    float3 positionWS; // World space position
    float3 positionVS; // View space position
    float4 positionCS; // Homogeneous clip space position
    float4 positionNDC; // Homogeneous normalized device coordinates
};

struct VertexNormalInputs
{
    real3 tangentWS;
    real3 bitangentWS;
    float3 normalWS;
};

VertexPositionInputs GetVertexPositionInputs(const float3 positionOS)
{
    VertexPositionInputs input;
    input.positionWS = TransformObjectToWorld(positionOS);
    input.positionVS = TransformWorldToView(input.positionWS);
    input.positionCS = TransformWorldToHClip(input.positionWS);

    float4 ndc = input.positionCS * 0.5f;
    input.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
    input.positionNDC.zw = input.positionCS.zw;

    return input;
}

VertexNormalInputs GetVertexNormalInputs(const float3 normalOS)
{
    VertexNormalInputs tbn;
    tbn.tangentWS = real3(1.0, 0.0, 0.0);
    tbn.bitangentWS = real3(0.0, 1.0, 0.0);
    tbn.normalWS = TransformObjectToWorldNormal(normalOS);
    return tbn;
}

VertexNormalInputs GetVertexNormalInputs(const float3 normalOs, const float4 tangentOs)
{
    VertexNormalInputs tbn;

    // mikkts space compliant. only normalize when extracting normal at frag.
    const real sign = real(tangentOs.w) * GetOddNegativeScale();
    tbn.normalWS = TransformObjectToWorldNormal(normalOs);
    tbn.tangentWS = real3(TransformObjectToWorldDir(tangentOs.xyz));
    tbn.bitangentWS = real3(cross(tbn.normalWS, float3(tbn.tangentWS))) * sign;
    return tbn;
}

#if defined(UNITY_SINGLE_PASS_STEREO)
float2 TransformStereoScreenSpaceTex(float2 uv, float w)
{
    float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
    return uv.xy * scaleOffset.xy + scaleOffset.zw * w;
}

float2 UnityStereoTransformScreenSpaceTex(float2 uv)
{
    return TransformStereoScreenSpaceTex(saturate(uv), 1.0);
}
#else
#define UnityStereoTransformScreenSpaceTex(uv) uv
#endif // defined(UNITY_SINGLE_PASS_STEREO)

#endif // TOON_RP_COMMON