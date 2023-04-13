#ifndef TOON_RP_ALPHA_CLIPPING
#define TOON_RP_ALPHA_CLIPPING

void AlphaClip(const float4 albedo)
{
    #ifdef _ALPHATEST_ON
    clip(albedo.a - _AlphaClipThreshold);
    #endif // _ALPHATEST_ON
}

#endif // TOON_RP_ALPHA_CLIPPING