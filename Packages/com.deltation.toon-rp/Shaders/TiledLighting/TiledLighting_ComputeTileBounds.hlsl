#include "TiledLighting_Shared.hlsl"

RWStructuredBuffer<TiledLighting_TileBounds> _TiledLighting_TileBounds;

#define COMPUTE_FRUSTUMS_GROUP_SIZE 16

float3 GetViewSpaceTiledCenter(const uint2 tileCoords, const bool orthographicCamera)
{
    if (orthographicCamera)
    {
        const float4 screenSpacePos = float4(tileCoords * TILE_SIZE + 0.5f * uint2(TILE_SIZE, TILE_SIZE), 0.0f, 1.0f);
        return TiledLighting_ScreenToView(screenSpacePos).xyz;
    }
    // ReSharper disable once CppRedundantElseKeywordInsideCompoundStatement (to prevent a wierd warning from the Unity's shader compiler)
    else
    {
        return float3(0.0f, 0.0f, 0.0f);
    }
}

float3 NormalizeZ(float3 v, const bool orthographicCamera)
{
    v = normalize(v);
    v = orthographicCamera ? v : v / v.z;
    v.z = 1;
    return v;
}

[numthreads(COMPUTE_FRUSTUMS_GROUP_SIZE, COMPUTE_FRUSTUMS_GROUP_SIZE, 1)]
void CS(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint2 cornerPixelCoords[TILED_LIGHTING_TILE_CORNERS_COUNT];

    const uint2 tileCoords = dispatchThreadId.xy;
    const uint2 topLeftPixelCoords = tileCoords * TILE_SIZE;
    // top-left
    cornerPixelCoords[0] = topLeftPixelCoords;
    // top-right
    cornerPixelCoords[1] = topLeftPixelCoords + uint2(TILE_SIZE, 0);
    // bottom-left
    cornerPixelCoords[2] = topLeftPixelCoords + uint2(0, TILE_SIZE);
    // bottom-right
    cornerPixelCoords[3] = topLeftPixelCoords + uint2(TILE_SIZE, TILE_SIZE);

    const bool orthographicCamera = IsOrthographicCamera();
    TiledLighting_TileBounds tileBounds;

    UNITY_BRANCH
    if (orthographicCamera)
    {
        float3 cornersViewSpaceNear[TILED_LIGHTING_TILE_CORNERS_COUNT];

        UNITY_UNROLL
        for (uint i = 0; i < TILED_LIGHTING_TILE_CORNERS_COUNT; i++)
        {
            float2 pixelCoords = (float2)cornerPixelCoords[i];
            cornersViewSpaceNear[i] =
                TiledLighting_ScreenToView(float4(pixelCoords, UNITY_NEAR_CLIP_VALUE, 1.0f)).xyz;
        }

        tileBounds.frustumCorners[0] = float3(cornersViewSpaceNear[0].xy, 0);
        tileBounds.frustumCorners[1] = float3(cornersViewSpaceNear[1].xy, 0);
        tileBounds.frustumCorners[2] = float3(cornersViewSpaceNear[2].xy, 0);
        tileBounds.frustumCorners[3] = float3(cornersViewSpaceNear[3].xy, 0);
    }
    else
    {
        tileBounds.frustumCorners[0] = 0;
        tileBounds.frustumCorners[1] = 0;
        tileBounds.frustumCorners[2] = 0;
        tileBounds.frustumCorners[3] = 0;
    }

    {
        float3 cornersViewSpaceFar[TILED_LIGHTING_TILE_CORNERS_COUNT];

        UNITY_UNROLL
        for (uint i = 0; i < TILED_LIGHTING_TILE_CORNERS_COUNT; i++)
        {
            float2 pixelCoords = (float2)cornerPixelCoords[i];
            cornersViewSpaceFar[i] =
                TiledLighting_ScreenToView(float4(pixelCoords, UNITY_RAW_FAR_CLIP_VALUE, 1.0f)).xyz;
        }

        tileBounds.frustumDirections[0] = NormalizeZ(cornersViewSpaceFar[0], orthographicCamera);
        tileBounds.frustumDirections[1] = NormalizeZ(cornersViewSpaceFar[1], orthographicCamera);
        tileBounds.frustumDirections[2] = NormalizeZ(cornersViewSpaceFar[2], orthographicCamera);
        tileBounds.frustumDirections[3] = NormalizeZ(cornersViewSpaceFar[3], orthographicCamera);
    }

    if (tileCoords.x < _TiledLighting_TilesX && tileCoords.y < _TiledLighting_TilesY)
    {
        const uint frustumIndex = TiledLighting_GetFlatTileIndex(tileCoords.x, tileCoords.y);
        _TiledLighting_TileBounds[frustumIndex] = tileBounds;
    }
}