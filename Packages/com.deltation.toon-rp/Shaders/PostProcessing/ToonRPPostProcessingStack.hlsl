#ifndef TOON_RP_POST_PROCESSING_STACK
#define TOON_RP_POST_PROCESSING_STACK

#include "Packages/com.deltation.toon-rp/ShaderLibrary/Common.hlsl"
#include "Packages/com.deltation.toon-rp/ShaderLibrary/Textures.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
#include "ToonRPPostProcessingStackCommon.hlsl"

#if defined(_FXAA_LOW)
#include "ToonRPFXAALowQuality.hlsl"
#elif defined(_FXAA_HIGH)
#include "ToonRPFXAAHighQuality.hlsl"
#endif

#include "ToonRPToneMapping.hlsl"
#include "ToonRPVignette.hlsl"
#include "ToonRPLookupTable.hlsl"
#include "ToonRPFilmGrain.hlsl"

float4 Frag(const Varyings IN) : SV_TARGET
{
    float3 color;

    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
    const float2 uv = UnityStereoTransformScreenSpaceTex(IN.texcoord);

    #ifdef _FXAA
    color = ApplyFxaa(uv);
    #else // !_FXAA
    color = SampleSource(uv);
    #endif // _FXAA

    #ifdef _TONE_MAPPING
    color = ApplyToneMapping(color);
    #else // !_TONE_MAPPING
    color = saturate(color);
    #endif // _TONE_MAPPING

    #ifdef _VIGNETTE
    color = ApplyVignette(color, uv);
    #endif // _VIGNETTE

    #ifdef _LOOKUP_TABLE
    color = ApplyLookupTable(color);
    #endif // _LOOKUP_TABLE

    #ifdef _FILM_GRAIN
    color = ApplyFilmGrain(uv, color);
    #endif // _FILM_GRAIN

    return float4(color, 1.0f);
}

#endif // TOON_RP_POST_PROCESSING_STACK