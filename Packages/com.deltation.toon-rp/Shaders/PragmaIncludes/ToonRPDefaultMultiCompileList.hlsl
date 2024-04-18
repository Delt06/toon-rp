#include_with_pragmas "ToonRPDefaultBaseMultiCompileList.hlsl"

#pragma multi_compile_fog

// Global Ramp
#pragma multi_compile_fragment _ _TOON_RP_GLOBAL_RAMP_CRISP _TOON_RP_GLOBAL_RAMP_TEXTURE

// Shadows
#pragma multi_compile _ _TOON_RP_DIRECTIONAL_SHADOWS _TOON_RP_DIRECTIONAL_CASCADED_SHADOWS _TOON_RP_BLOB_SHADOWS
#pragma multi_compile _ _TOON_RP_ADDITIONAL_SHADOWS
#pragma multi_compile_fragment _ _TOON_RP_PCF _TOON_RP_VSM
#pragma multi_compile_fragment _ _TOON_RP_POISSON_SAMPLING_STRATIFIED _TOON_RP_POISSON_SAMPLING_ROTATED
#pragma multi_compile_fragment _ _TOON_RP_POISSON_SAMPLING_EARLY_BAIL
#pragma multi_compile_fragment _ _TOON_RP_SHADOWS_RAMP_CRISP
#pragma multi_compile_fragment _ _TOON_RP_SHADOWS_PATTERN

// Light
#pragma multi_compile _ _TOON_RP_TILED_LIGHTING _TOON_RP_ADDITIONAL_LIGHTS _TOON_RP_ADDITIONAL_LIGHTS_VERTEX

// GI
#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
#pragma multi_compile _ SHADOWS_SHADOWMASK
#pragma multi_compile _ DIRLIGHTMAP_COMBINED
#pragma multi_compile _ LIGHTMAP_ON

// SSAO
#pragma multi_compile_fragment _ _TOON_RP_SSAO _TOON_RP_SSAO_PATTERN