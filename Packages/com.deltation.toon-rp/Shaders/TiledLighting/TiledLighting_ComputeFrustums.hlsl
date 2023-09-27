#include "TiledLighting_Shared.hlsl"

RWStructuredBuffer<TiledLighting_Frustum> _TiledLighting_Frustums;

#define COMPUTE_FRUSTUMS_GROUP_SIZE 16

[numthreads(COMPUTE_FRUSTUMS_GROUP_SIZE, COMPUTE_FRUSTUMS_GROUP_SIZE, 1)]
void CS(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    float4 screenSpace[4];
    // far clipping plane
    const float screenSpaceZ = 1.0f;
    const uint2 tileCoords = dispatchThreadId.xy;
    // top left
    screenSpace[0] = float4(tileCoords * TILE_SIZE, screenSpaceZ, 1.0f);
    // top right
    screenSpace[1] = float4(tileCoords * TILE_SIZE + uint2(TILE_SIZE, 0), screenSpaceZ, 1.0f);
    // bottom left
    screenSpace[2] = float4(tileCoords * TILE_SIZE + uint2(0, TILE_SIZE), screenSpaceZ, 1.0f);
    // bottom right
    screenSpace[3] = float4(tileCoords * TILE_SIZE + uint2(TILE_SIZE, TILE_SIZE), screenSpaceZ, 1.0f);

    float3 viewSpace[4];
    for (uint i = 0; i < 4; i++)
    {
        viewSpace[i] = TiledLighting_ScreenToView(screenSpace[i]).xyz;
    }

    const float3 eyePos = float3(0, 0, 0);
    TiledLighting_Frustum frustum;
    // Left plane
    frustum.planes[0] = ComputePlane(eyePos, viewSpace[2], viewSpace[0]);
    // Right plane
    frustum.planes[1] = ComputePlane(eyePos, viewSpace[1], viewSpace[3]);
    // Top plane
    frustum.planes[2] = ComputePlane(eyePos, viewSpace[0], viewSpace[1]);
    // Bottom plane
    frustum.planes[3] = ComputePlane(eyePos, viewSpace[3], viewSpace[2]);

    if (tileCoords.x < _TiledLighting_TilesX && tileCoords.y < _TiledLighting_TilesY)
    {
        const uint frustumIndex = TiledLighting_GetFlatTileIndex(tileCoords.x, tileCoords.y);
        _TiledLighting_Frustums[frustumIndex] = frustum;
    }
}