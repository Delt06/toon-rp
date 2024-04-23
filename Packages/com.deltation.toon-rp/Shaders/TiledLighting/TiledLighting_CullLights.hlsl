#include "TiledLighting_Shared.hlsl"

StructuredBuffer<TiledLighting_TileBounds> _TiledLighting_TileBounds;
RWStructuredBuffer<uint> _TiledLighting_LightIndexList;
RWStructuredBuffer<uint2> _TiledLighting_LightGrid;

groupshared uint g_MinDepth;
groupshared uint g_MaxDepth;
groupshared TiledLighting_TileBounds g_TileBounds;

#define MAX_LIGHTS_PER_TILE 64

groupshared uint g_LightList_Count_Opaque;
groupshared uint g_LightList_IndexStartOffset_Opaque;
groupshared uint g_LightList_Opaque[MAX_LIGHTS_PER_TILE];

void AppendLight_Opaque(const uint lightIndex)
{
    uint index;
    InterlockedAdd(g_LightList_Count_Opaque, 1, index);

    if (index < _TiledLighting_ReservedLightsPerTile)
    {
        g_LightList_Opaque[index] = lightIndex;
    }
}

float RemapDepthToClipZ(const float depth)
{
    if (UNITY_NEAR_CLIP_VALUE == -1)
    {
        // ReSharper disable once CppUnreachableCode
        return depth * 2 - 1;
    }

    return depth;
}

TiledLighting_Aabb ComputeFrustumSliceAabb(const TiledLighting_TileBounds tileBounds, const float minDepthVs,
                                           const float maxDepthVs)
{
    float3 aabbMin = FLT_INF, aabbMax = -FLT_INF;
    const bool orthographicCamera = IsOrthographicCamera();

    UNITY_UNROLL
    for (uint i = 0; i < 4; ++i)
    {
        const float3 corner = orthographicCamera ? tileBounds.frustumCorners[i] : float3(0, 0, 0);
        const float3 direction = tileBounds.frustumDirections[i];
        aabbMin = min(aabbMin, min(corner + direction * minDepthVs, corner + direction * maxDepthVs));
        aabbMax = max(aabbMax, max(corner + direction * minDepthVs, corner + direction * maxDepthVs));
    }

    TiledLighting_Aabb aabb;
    aabb.center = (aabbMin + aabbMax) * 0.5f;
    aabb.extents = (aabbMax - aabbMin) * 0.5f;
    return aabb;
}

[numthreads(TILE_SIZE, TILE_SIZE, 1)]
void CS(
    const uint3 dispatchThreadId : SV_DispatchThreadID,
    const uint localIndex : SV_GroupIndex,
    const uint3 groupId : SV_GroupID
)
{
    int2 pixelCoord = dispatchThreadId.xy;
    #ifdef UNITY_UV_STARTS_AT_TOP
    pixelCoord.y = (int)_TiledLighting_ScreenDimensions.y - 1 - pixelCoord.y;
    #endif // UNITY_UV_STARTS_AT_TOP 
    const float depth = _ToonRP_DepthTexture.Load(int3(pixelCoord, 0)).r;

    const uint depthAsUint = asuint(depth);
    if (localIndex == 0)
    {
        g_MinDepth = 0xFFFFFFFF;
        g_MaxDepth = 0;
        g_LightList_Count_Opaque = 0;
        g_TileBounds = _TiledLighting_TileBounds[TiledLighting_GetFlatTileIndex(groupId.x, groupId.y)];
    }

    GroupMemoryBarrierWithGroupSync();

    InterlockedMin(g_MinDepth, depthAsUint);
    InterlockedMax(g_MaxDepth, depthAsUint);

    GroupMemoryBarrierWithGroupSync();

    const float minDepth = asfloat(g_MinDepth);
    const float maxDepth = asfloat(g_MaxDepth);

    // ReSharper disable once CppLocalVariableMayBeConst
    float minDepthVs = TiledLighting_ClipToView(float4(0, 0, RemapDepthToClipZ(minDepth), 1)).z;
    // ReSharper disable once CppLocalVariableMayBeConst
    float maxDepthVs = TiledLighting_ClipToView(float4(0, 0, RemapDepthToClipZ(maxDepth), 1)).z;

    #ifdef UNITY_REVERSED_Z
    Swap(minDepthVs, maxDepthVs);
    #endif // UNITY_REVERSED_Z

    const TiledLighting_Aabb opaqueAabb = ComputeFrustumSliceAabb(g_TileBounds, minDepthVs, maxDepthVs);

    uint i;

    for (i = localIndex; i < _AdditionalLightCount; i += TILE_SIZE * TILE_SIZE)
    {
        const TiledLight light = _TiledLighting_Lights_SB[i];

        TiledLighting_Sphere boundingSphere;
        boundingSphere.center = light.BoundingSphere_CenterVs_Radius.xyz;
        boundingSphere.radius = light.BoundingSphere_CenterVs_Radius.w;

        if (TiledLighting_SphereInsideAabb(boundingSphere, opaqueAabb))
        {
            TiledLighting_Sphere coneBoundingSphere;
            coneBoundingSphere.center = light.ConeBoundingSphere_CenterVs_Radius.xyz;
            coneBoundingSphere.radius = light.ConeBoundingSphere_CenterVs_Radius.w;
            
            if (coneBoundingSphere.radius == 0.0f ||
                TiledLighting_SphereInsideAabb(coneBoundingSphere, opaqueAabb))
            {
                AppendLight_Opaque(i);
            }
        }
    }

    GroupMemoryBarrierWithGroupSync();

    if (localIndex == 0)
    {
        const uint tileIndex = TiledLighting_GetFlatTileIndex(groupId.x, groupId.y);

        g_LightList_Count_Opaque = min(_TiledLighting_ReservedLightsPerTile, g_LightList_Count_Opaque);
        InterlockedAdd(_TiledLighting_LightIndexList[0],
                       g_LightList_Count_Opaque,
                       g_LightList_IndexStartOffset_Opaque);
        _TiledLighting_LightGrid[TiledLighting_GetOpaqueLightGridIndex(tileIndex)] = uint2(
            g_LightList_IndexStartOffset_Opaque,
            g_LightList_Count_Opaque);
    }

    GroupMemoryBarrierWithGroupSync();

    for (i = localIndex; i < g_LightList_Count_Opaque; i += TILE_SIZE * TILE_SIZE)
    {
        const uint tileIndex = g_LightList_IndexStartOffset_Opaque + i;
        const uint index = TiledLighting_GetOpaqueLightIndexListIndex(tileIndex);
        _TiledLighting_LightIndexList[index] = g_LightList_Opaque[i];
    }
}