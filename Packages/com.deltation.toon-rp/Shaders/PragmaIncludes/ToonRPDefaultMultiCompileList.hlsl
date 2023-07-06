﻿#include_with_pragmas "ToonRPDefaultBaseMultiCompileList.hlsl"

#pragma multi_compile_fog

// Global Ramp
#pragma multi_compile_fragment _ _TOON_RP_GLOBAL_RAMP_CRISP _TOON_RP_GLOBAL_RAMP_TEXTURE

// Shadows
#pragma multi_compile _ _TOON_RP_DIRECTIONAL_SHADOWS _TOON_RP_DIRECTIONAL_CASCADED_SHADOWS _TOON_RP_BLOB_SHADOWS
#pragma multi_compile_fragment _ _TOON_RP_PCF _TOON_RP_VSM
#pragma multi_compile_fragment _ _TOON_RP_SHADOWS_RAMP_CRISP
#pragma multi_compile_fragment _ _TOON_RP_SHADOWS_PATTERN

// Lights
#pragma multi_compile _ _TOON_RP_ADDITIONAL_LIGHTS _TOON_RP_ADDITIONAL_LIGHTS_VERTEX

// SSAO
#pragma multi_compile_fragment _ _TOON_RP_SSAO _TOON_RP_SSAO_PATTERN