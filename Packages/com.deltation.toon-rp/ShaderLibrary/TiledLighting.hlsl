﻿#ifndef TOON_RP_TILED_LIGHTING
#define TOON_RP_TILED_LIGHTING

#include "../ShaderLibrary/Lighting.hlsl"
#include "../Shaders/TiledLighting/TiledLighting_Shared.hlsl"

StructuredBuffer<uint> _TiledLighting_LightIndexList;
StructuredBuffer<uint2> _TiledLighting_LightGrid;

uint2 TiledLighting_ScreenPositionToTileIndex(float2 screenPosition)
{
    screenPosition.y = _TiledLighting_ScreenDimensions.y - screenPosition.y;
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
    const uint2 lightGridValue = _TiledLighting_LightGrid[_TiledLighting_CurrentLightGridOffset + flatTileIndex];
    TiledLighting_LightGridCell cell;
    cell.indexStartOffset = lightGridValue.x;
    cell.lightCount = lightGridValue.y;
    return cell;
}

Light GetAdditionalLightTiled(const uint perTileLightIndex, const TiledLighting_LightGridCell cell,
                              const float3 positionWs)
{
    const uint globalLightIndex = _TiledLighting_LightIndexList[_TiledLighting_CurrentLightIndexListOffset + cell.
        indexStartOffset + perTileLightIndex];
    return GetAdditionalLightGlobal(globalLightIndex, positionWs);
}

#endif // TOON_RP_TILED_LIGHTING