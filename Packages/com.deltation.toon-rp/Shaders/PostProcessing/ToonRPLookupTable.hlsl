#ifndef TOON_RP_LOOKUP_TABLE
#define TOON_RP_LOOKUP_TABLE

#include "ToonRPPostProcessingStackCommon.hlsl"

float3 ApplyLookupTable(const float3 previousColor)
{
    // Based on ApplyLut2D from Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl

    // scaleOffset = (1 / lut_width, 1 / lut_height, lut_height - 1)
    const float3 scaleOffset = float3(
        _LookupTable_Texture_TexelSize.x,
        _LookupTable_Texture_TexelSize.y,
        _LookupTable_Texture_TexelSize.w - 1
    );
    float3 color = previousColor;
    color.z *= scaleOffset.z;
    const float shift = floor(color.z);
    color.xy = color.xy * scaleOffset.z * scaleOffset.xy + scaleOffset.xy * 0.5;
    color.x += shift * scaleOffset.y;
    color.xyz = lerp(
        SAMPLE_TEXTURE2D_LOD(_LookupTable_Texture, sampler_LookupTable_Texture, color.xy, 0.0).rgb,
        SAMPLE_TEXTURE2D_LOD(_LookupTable_Texture, sampler_LookupTable_Texture, color.xy + float2(scaleOffset.y, 0.0),
                             0.0).rgb,
        color.z - shift
    );
    return color;
}

#endif // TOON_RP_LOOKUP_TABLE