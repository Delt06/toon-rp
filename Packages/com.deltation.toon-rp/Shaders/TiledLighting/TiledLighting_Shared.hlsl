#ifndef TOON_RP_TILED_LIGHTING_SHARED
#define TOON_RP_TILED_LIGHTING_SHARED

// https://www.3dgep.com/forward-plus/
#define TILE_SIZE 16
#define RESERVED_LIGHTS_PER_TILE 2

#include "../../ShaderLibrary/Common.hlsl"
#include "../../ShaderLibrary/Lighting.hlsl"

CBUFFER_START(TiledLighting)
    float2 _TiledLighting_ScreenDimensions;
    uint _TiledLighting_TilesX;
    uint _TiledLighting_TilesY;

    uint _TiledLighting_CurrentLightIndexListOffset;
    uint _TiledLighting_CurrentLightGridOffset;
CBUFFER_END

struct TiledLighting_FrustumVectors
{
    float3 topLeft;
    float3 topRight;
    float3 bottomLeft;
    float3 bottomRight;
};

struct TiledLighting_AABB
{
    float3 center;
    float3 halfSize;
};

struct TiledLighting_Sphere
{
    float3 center;
    float radius;
};

TiledLighting_AABB TiledLighting_ComputeFrustumAABB(
    const TiledLighting_FrustumVectors frustumVectors,
    const float zNear, const float zFar)
{
    float3 corners[8];
    corners[0] = frustumVectors.topLeft * zNear;
    corners[1] = frustumVectors.topRight * zNear;
    corners[2] = frustumVectors.bottomLeft * zNear;
    corners[3] = frustumVectors.bottomRight * zNear;
    corners[4] = frustumVectors.topLeft * zFar;
    corners[5] = frustumVectors.topRight * zFar;
    corners[6] = frustumVectors.bottomLeft * zFar;
    corners[7] = frustumVectors.bottomRight * zFar;

    float3 aabbMin = corners[0];
    float3 aabbMax = corners[0];

    UNITY_UNROLL
    for (uint i = 1; i < 8; ++i)
    {
        aabbMin = min(aabbMin, corners[i]);
        aabbMax = max(aabbMax, corners[i]);
    }

    TiledLighting_AABB aabb;
    aabb.halfSize = (aabbMax - aabbMin) * 0.5f;
    aabb.center = aabbMin + aabb.halfSize;
    return aabb;
}

bool TiledLighting_TestSphereVsAABB(const TiledLighting_Sphere sphere, const TiledLighting_AABB aabb)
{
    const float3 delta = max(0, abs(aabb.center - sphere.center) - aabb.halfSize);
    const float distSq = dot(delta, delta);
    return distSq <= sphere.radius * sphere.radius;
}

// Convert clip space coordinates to view space
float4 TiledLighting_ClipToView(const float4 clip)
{
    // View space position.
    float4 view = mul(UNITY_MATRIX_I_P, clip);
    // Perspective projection.
    view = view / view.w;

    return view;
}

float4 TiledLighting_ScreenToView(const float4 screenCoordinates)
{
    // Convert to normalized texture coordinates
    float2 texCoord = screenCoordinates.xy / _TiledLighting_ScreenDimensions;

    #ifdef UNITY_UV_STARTS_AT_TOP
    texCoord.y = 1.0f - texCoord.y;
    #endif // UNITY_UV_STARTS_AT_TOP

    // Convert to clip space

    const float4 positionCs = float4(float2(texCoord.x, 1.0f - texCoord.y) * 2.0f - 1.0f, screenCoordinates.z,
                                     screenCoordinates.w);
    return TiledLighting_ClipToView(positionCs);
}

uint TiledLighting_GetFlatTileIndex(const uint tileX, const uint tileY)
{
    return tileY * _TiledLighting_TilesX + tileX;
}

uint TiledLighting_GetOpaqueLightGridIndex(const uint tileIndex)
{
    return tileIndex;
}

uint TiledLighting_GetTransparentLightGridIndex(const uint tileIndex)
{
    return _TiledLighting_TilesX * _TiledLighting_TilesY + tileIndex;
}

uint TiledLighting_GetOpaqueLightIndexListIndex(const uint tileIndex)
{
    return tileIndex;
}

uint TiledLighting_GetTransparentLightIndexListIndex(const uint tileIndex)
{
    return _TiledLighting_TilesX * _TiledLighting_TilesY * RESERVED_LIGHTS_PER_TILE + tileIndex;
}

#endif // TOON_RP_TILED_LIGHTING_SHARED