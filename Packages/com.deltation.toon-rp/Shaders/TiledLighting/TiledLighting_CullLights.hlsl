#include "TiledLighting_Shared.hlsl"

#include "../../ShaderLibrary/DepthNormals.hlsl"

StructuredBuffer<TiledLighting_FrustumVectors> _TiledLighting_Frustums;
RWStructuredBuffer<uint> _TiledLighting_LightIndexCounter;
RWStructuredBuffer<uint> _TiledLighting_LightIndexList;
RWStructuredBuffer<uint2> _TiledLighting_LightGrid;

groupshared uint g_MinDepth;
groupshared uint g_MaxDepth;
groupshared TiledLighting_FrustumVectors g_FrustumVector;
groupshared TiledLighting_AABB g_FrustumAabb_Opaque;
groupshared TiledLighting_AABB g_FrustumAabb_Transparent;

#define MAX_LIGHTS_PER_TILE 1024

groupshared uint g_LightList_Count_Opaque;
groupshared uint g_LightList_IndexStartOffset_Opaque;
groupshared uint g_LightList_Opaque[MAX_LIGHTS_PER_TILE];

groupshared uint g_LightList_Count_Transparent;
groupshared uint g_LightList_IndexStartOffset_Transparent;
groupshared uint g_LightList_Transparent[MAX_LIGHTS_PER_TILE];

void AppendLight_Opaque(const uint lightIndex)
{
    uint index;
    InterlockedAdd(g_LightList_Count_Opaque, 1, index);

    if (index < MAX_LIGHTS_PER_TILE)
    {
        g_LightList_Opaque[index] = lightIndex;
    }
}

void AppendLight_Transparent(const uint lightIndex)
{
    uint index;
    InterlockedAdd(g_LightList_Count_Transparent, 1, index);

    if (index < MAX_LIGHTS_PER_TILE)
    {
        g_LightList_Transparent[index] = lightIndex;
    }
}

[numthreads(TILE_SIZE, TILE_SIZE, 1)]
void CS(
    const uint3 dispatchThreadId : SV_DispatchThreadID,
    const uint groupIndex : SV_GroupIndex,
    const uint3 groupId : SV_GroupID
)
{
    int2 pixelCoord = dispatchThreadId.xy;
    #ifdef UNITY_UV_STARTS_AT_TOP
    pixelCoord.y = (int)_TiledLighting_ScreenDimensions.y - 1 - pixelCoord.y;
    #endif // UNITY_UV_STARTS_AT_TOP 
    const float depth = _ToonRP_DepthTexture.Load(int3(pixelCoord, 0)).r;

    const uint depthAsUint = asuint(depth);
    if (groupIndex == 0)
    {
        g_MinDepth = 0xFFFFFFFF;
        g_MaxDepth = 0;
        g_LightList_Count_Opaque = 0;
        g_LightList_Count_Transparent = 0;
        g_FrustumVector = _TiledLighting_Frustums[TiledLighting_GetFlatTileIndex(groupId.x, groupId.y)];
    }

    GroupMemoryBarrierWithGroupSync();

    InterlockedMin(g_MinDepth, depthAsUint);
    InterlockedMax(g_MaxDepth, depthAsUint);

    GroupMemoryBarrierWithGroupSync();

    if (groupIndex == 0)
    {
        const float minDepth = asfloat(g_MinDepth);
        const float maxDepth = asfloat(g_MaxDepth);

        float minDepthVs = TiledLighting_ClipToView(float4(0, 0, minDepth, 1)).z;
        float maxDepthVs = TiledLighting_ClipToView(float4(0, 0, maxDepth, 1)).z;
        const float nearClipVs = TiledLighting_ClipToView(float4(0, 0, 0, 1)).z;

        #ifdef UNITY_REVERSED_Z
        minDepthVs *= -1;
        maxDepthVs *= -1;
        #endif // UNITY_REVERSED_Z

        g_FrustumAabb_Opaque = TiledLighting_ComputeFrustumAABB(g_FrustumVector, minDepthVs, maxDepthVs);
        g_FrustumAabb_Transparent = TiledLighting_ComputeFrustumAABB(g_FrustumVector, nearClipVs, maxDepthVs);
    }

    GroupMemoryBarrierWithGroupSync();

    uint i;

    for (i = groupIndex; i < _AdditionalLightCount; i += TILE_SIZE * TILE_SIZE)
    {
        const float4 positionsEntry = _AdditionalLightPositionsVS[i];
        const float3 positionVs = positionsEntry.xyz;
        const float range = positionsEntry.w;

        TiledLighting_Sphere boundingSphere;
        boundingSphere.center = positionVs;
        boundingSphere.radius = range;

        if (TiledLighting_TestSphereVsAABB(boundingSphere, g_FrustumAabb_Opaque))
        {
            AppendLight_Opaque(i);
        }

        if (TiledLighting_TestSphereVsAABB(boundingSphere, g_FrustumAabb_Transparent))
        {
            AppendLight_Transparent(i);
        }
    }

    GroupMemoryBarrierWithGroupSync();

    if (groupIndex == 0)
    {
        const uint tileIndex = TiledLighting_GetFlatTileIndex(groupId.x, groupId.y);

        InterlockedAdd(_TiledLighting_LightIndexCounter[0],
                       g_LightList_Count_Opaque,
                       g_LightList_IndexStartOffset_Opaque);
        _TiledLighting_LightGrid[TiledLighting_GetOpaqueLightGridIndex(tileIndex)] = uint2(
            g_LightList_IndexStartOffset_Opaque,
            g_LightList_Count_Opaque);

        InterlockedAdd(_TiledLighting_LightIndexCounter[1],
                       g_LightList_Count_Transparent,
                       g_LightList_IndexStartOffset_Transparent);
        _TiledLighting_LightGrid[TiledLighting_GetTransparentLightGridIndex(tileIndex)] = uint2(
            g_LightList_IndexStartOffset_Transparent,
            g_LightList_Count_Transparent);
    }

    GroupMemoryBarrierWithGroupSync();

    for (i = groupIndex; i < g_LightList_Count_Opaque; i += TILE_SIZE * TILE_SIZE)
    {
        const uint index = TiledLighting_GetOpaqueLightIndexListIndex(g_LightList_IndexStartOffset_Opaque + i);
        _TiledLighting_LightIndexList[index] = g_LightList_Opaque[i];
    }

    for (i = groupIndex; i < g_LightList_Count_Transparent; i += TILE_SIZE * TILE_SIZE)
    {
        const uint index =
            TiledLighting_GetTransparentLightIndexListIndex(g_LightList_IndexStartOffset_Transparent + i);
        _TiledLighting_LightIndexList[index] = g_LightList_Transparent[i];
    }
}