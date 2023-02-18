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

#define CONSTRUCT_TILING_OFFSET_NAME(textureName) textureName ## _ST
#define DECLARE_TILING_OFFSET(textureName) float4 CONSTRUCT_TILING_OFFSET_NAME(textureName);
#define APPLY_TILING_OFFSET(uv, textureName) (uv) * (CONSTRUCT_TILING_OFFSET_NAME(textureName).xy) + (CONSTRUCT_TILING_OFFSET_NAME(textureName).zw) 

#endif // TOON_RP_COMMON