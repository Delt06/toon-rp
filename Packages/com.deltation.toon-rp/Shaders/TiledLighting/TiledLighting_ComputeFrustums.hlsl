#include "TiledLighting_Shared.hlsl"

RWStructuredBuffer<TiledLighting_FrustumVectors> _TiledLighting_Frustums;

#define COMPUTE_FRUSTUMS_GROUP_SIZE 16

float3 ScaleByInverseCosine(const float3 vec, const float3 forward)
{
    const float cosine = dot(vec, forward);
    return vec / cosine;
}

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

    const float3 forward = float3(0, 0, -1);

    TiledLighting_FrustumVectors frustumVectors;
    frustumVectors.topLeft = ScaleByInverseCosine(normalize(viewSpace[0]), forward);
    frustumVectors.topRight = ScaleByInverseCosine(normalize(viewSpace[1]), forward);
    frustumVectors.bottomLeft = ScaleByInverseCosine(normalize(viewSpace[2]), forward);
    frustumVectors.bottomRight = ScaleByInverseCosine(normalize(viewSpace[3]), forward);

    if (tileCoords.x < _TiledLighting_TilesX && tileCoords.y < _TiledLighting_TilesY)
    {
        const uint frustumIndex = TiledLighting_GetFlatTileIndex(tileCoords.x, tileCoords.y);
        _TiledLighting_Frustums[frustumIndex] = frustumVectors;
    }
}