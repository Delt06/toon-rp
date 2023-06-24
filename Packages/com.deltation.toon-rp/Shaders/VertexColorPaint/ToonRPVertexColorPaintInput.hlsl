#ifndef TOON_RP_VERTEX_COLOR_PAINT_INPUT
#define TOON_RP_VERTEX_COLOR_PAINT_INPUT

#include "../../ShaderLibrary/Common.hlsl"

CBUFFER_START(UnityPerMaterial)

float _DiffuseIntensity0;
float _DiffuseIntensity1;

CBUFFER_END

#endif // TOON_RP_VERTEX_COLOR_PAINT_INPUT