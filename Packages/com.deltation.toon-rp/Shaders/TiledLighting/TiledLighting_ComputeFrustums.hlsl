#include "TiledLighting_Shared.hlsl"

RWStructuredBuffer<TiledLighting_Frustum> _TiledLighting_Frustums;

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

[numthreads(COMPUTE_FRUSTUMS_GROUP_SIZE, COMPUTE_FRUSTUMS_GROUP_SIZE, 1)]
void CS(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    float4 cornersScreenSpace[4];
    // far clipping plane
    const float screenSpaceZ = 1.0f;
    const uint2 tileCoords = dispatchThreadId.xy;
    // top left
    cornersScreenSpace[0] = float4(tileCoords * TILE_SIZE, screenSpaceZ, 1.0f);
    // top right
    cornersScreenSpace[1] = float4(tileCoords * TILE_SIZE + uint2(TILE_SIZE, 0), screenSpaceZ, 1.0f);
    // bottom left
    cornersScreenSpace[2] = float4(tileCoords * TILE_SIZE + uint2(0, TILE_SIZE), screenSpaceZ, 1.0f);
    // bottom right
    cornersScreenSpace[3] = float4(tileCoords * TILE_SIZE + uint2(TILE_SIZE, TILE_SIZE), screenSpaceZ, 1.0f);

    float3 cornersViewSpace[4];
    for (uint i = 0; i < 4; i++)
    {
        cornersViewSpace[i] = TiledLighting_ScreenToView(cornersScreenSpace[i]).xyz;
    }

    const bool orthographicCamera = IsOrthographicCamera();
    const float3 centerViewSpace = GetViewSpaceTiledCenter(tileCoords, orthographicCamera);
    TiledLighting_Frustum frustum;
    // Left plane
    frustum.planes[0] = ComputePlane(centerViewSpace, cornersViewSpace[2], cornersViewSpace[0], orthographicCamera);
    // Right plane
    frustum.planes[1] = ComputePlane(centerViewSpace, cornersViewSpace[1], cornersViewSpace[3], orthographicCamera);
    // Top plane
    frustum.planes[2] = ComputePlane(centerViewSpace, cornersViewSpace[0], cornersViewSpace[1], orthographicCamera);
    // Bottom plane
    frustum.planes[3] = ComputePlane(centerViewSpace, cornersViewSpace[3], cornersViewSpace[2], orthographicCamera);

    if (tileCoords.x < _TiledLighting_TilesX && tileCoords.y < _TiledLighting_TilesY)
    {
        const uint frustumIndex = TiledLighting_GetFlatTileIndex(tileCoords.x, tileCoords.y);
        _TiledLighting_Frustums[frustumIndex] = frustum;
    }
}