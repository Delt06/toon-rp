#ifndef TOON_RP_FILM_GRAIN
#define TOON_RP_FILM_GRAIN

#include "ToonRPPostProcessingStackCommon.hlsl"

float3 ApplyFilmGrain(const float2 uv, const float3 previousColor)
{
    const float2 pixelCoords = _MainTex_TexelSize.zw * uv;
    const float2 grainPixelCoords = pixelCoords / _FilmGrain_Texture_TexelSize.zw;
    const float2 grainUv = grainPixelCoords % 1;
    const float3 grain = SAMPLE_TEXTURE2D_LOD(_FilmGrain_Texture, sampler_FilmGrain_Texture, grainUv, 0).rgb;
    const float3 grainedColor = saturate(previousColor - grain * _FilmGrain_Intensity);
    const float lumaT = smoothstep(_FilmGrain_LuminanceThreshold0, _FilmGrain_LuminanceThreshold1,
                                   Luminance(previousColor));
    return lerp(grainedColor, previousColor, lumaT);
}

#endif // TOON_RP_FILM_GRAIN