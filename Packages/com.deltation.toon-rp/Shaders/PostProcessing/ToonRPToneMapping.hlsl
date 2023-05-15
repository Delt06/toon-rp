#ifndef TOON_RP_TONE_MAPPING
#define TOON_RP_TONE_MAPPING

#include "ToonRPPostProcessingStackCommon.hlsl"

float3 ApplyToneMapping(const float3 previousColor)
{
    return 1 - exp(-previousColor * _ToneMapping_Exposure);
}

#endif // TOON_RP_TONE_MAPPING