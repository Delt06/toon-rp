#ifndef TOON_RP_TILED_LIGHTING
#define TOON_RP_TILED_LIGHTING

#include "Lighting.hlsl"
#include "../Shaders/TiledLighting/TiledLighting_Shared.hlsl"

StructuredBuffer<uint> _TiledLighting_LightIndexList;
StructuredBuffer<uint2> _TiledLighting_LightGrid;

uint2 TiledLighting_ScreenPositionToTileIndex(float2 screenPosition)
{
    #ifdef TOON_PRETRANSFORM_TO_DISPLAY_ORIENTATION
    screenPosition = ApplyPretransformRotationPixelCoords2(screenPosition);
    #endif // TOON_PRETRANSFORM_TO_DISPLAY_ORIENTATION

    #ifdef UNITY_UV_STARTS_AT_TOP
    if (_ProjectionParams.x < 0.0)
    {
        screenPosition.y = _TiledLighting_ScreenDimensions.y - screenPosition.y;
    }
    #endif // UNITY_UV_STARTS_AT_TOP
    return uint2(floor(screenPosition / TILE_SIZE));
}

struct TiledLighting_LightGridCell
{
    uint indexStartOffset;
    uint lightCount;
};

TiledLighting_LightGridCell TiledLighting_GetLightGridCell(const float2 screenCoords)
{
    const uint2 tileIndex = TiledLighting_ScreenPositionToTileIndex(screenCoords);
    const uint flatTileIndex = TiledLighting_GetFlatTileIndex(tileIndex.x, tileIndex.y);
    const uint2 lightGridValue = _TiledLighting_LightGrid[flatTileIndex];
    TiledLighting_LightGridCell cell;
    cell.indexStartOffset = lightGridValue.x;
    cell.lightCount = lightGridValue.y;
    return cell;
}

LightEntry GetTiledLightEntry(const uint globalLightIndex)
{
    LightEntry lightEntry;
    lightEntry.color = _TiledLighting_Light_Color[globalLightIndex].rgb;

    const float4 positionWs_attenuation = _TiledLighting_Light_PositionsWs_Attenuation[globalLightIndex]; 
    lightEntry.positionWs = positionWs_attenuation.xyz;
    lightEntry.distanceAttenuation = positionWs_attenuation.w;

    const float4 spotDir_Attenuation = _TiledLighting_Light_SpotDir_Attenuation[globalLightIndex];
    lightEntry.spotDir = spotDir_Attenuation.xyz;
    const uint spotAttenuation = asuint(spotDir_Attenuation.w);
    lightEntry.spotAttenuation = half2((half) f16tof32(spotAttenuation >> 16), (half) f16tof32(spotAttenuation));

    lightEntry.shadowIndex = GetLightShadowIndex(globalLightIndex);
    
    return lightEntry;
}

Light GetAdditionalLightTiled(const uint perTileLightIndex, const TiledLighting_LightGridCell cell,
                              const float3 positionWs)
{
    const uint listIndex = TiledLighting_GetOpaqueLightIndexListIndex(cell.indexStartOffset + perTileLightIndex);
    const uint globalLightIndex = _TiledLighting_LightIndexList[listIndex];
    const LightEntry lightEntry = GetTiledLightEntry(globalLightIndex);
    return ConvertEntryToLight(lightEntry, positionWs);
}


#endif // TOON_RP_TILED_LIGHTING