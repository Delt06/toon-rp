#ifndef TOON_RP_TILED_LIGHTING_SHARED
#define TOON_RP_TILED_LIGHTING_SHARED

// https://www.3dgep.com/forward-plus/

#define TILE_SIZE 16

// Mirrored with ToonLighting.cs
#define MAX_ADDITIONAL_LIGHTS_COUNT_TILED 1024

// the counter is stored in the beginning of the buffer
#define LIGHT_INDEX_LIST_BASE_INDEX_OFFSET 1

#include "../../ShaderLibrary/Common.hlsl"
#include "../../ShaderLibrary/Lighting.hlsl"
#include "../../Runtime/Lighting/TiledLight.cs.hlsl"

CBUFFER_START(TiledLighting)
    float2 _TiledLighting_ScreenDimensions;
    uint _TiledLighting_TilesX;
    uint _TiledLighting_TilesY;

    uint _TiledLighting_ReservedLightsPerTile;
CBUFFER_END

StructuredBuffer<TiledLight> _TiledLighting_Lights_SB;

CBUFFER_START(TiledLighting_Lights_CB_Colors)
float4 _TiledLighting_Light_Color[MAX_ADDITIONAL_LIGHTS_COUNT_TILED];
CBUFFER_END

CBUFFER_START(TiledLighting_Lights_CB_PositionsAttenuations)
float4 _TiledLighting_Light_PositionsWs_Attenuation[MAX_ADDITIONAL_LIGHTS_COUNT_TILED];
CBUFFER_END

CBUFFER_START(TiledLighting_Lights_CB_SpotDirAttenuations)
float4 _TiledLighting_Light_SpotDir_Attenuation[MAX_ADDITIONAL_LIGHTS_COUNT_TILED];
CBUFFER_END

struct TiledLighting_Plane
{
    float3 normal;
    float distance;
};

TiledLighting_Plane ComputePlane(const float3 p0, const float3 p1, const float3 p2, const bool orthographicCamera)
{
    TiledLighting_Plane plane;

    const float3 v0 = p1 - p0;
    const float3 v2 = p2 - p0;

    plane.normal = cross(v0, v2);
    if (orthographicCamera)
    {
        // Frustum planes for orthographic cameras should always have normal.z == 0.
        // Due to floating point imprecision, it may end up non-zero.
        // Thus, we set it to 0 explicitly.
        plane.normal.z = 0.0f;
    }
    plane.normal = normalize(plane.normal);

    // Compute the distance to the origin using p0.
    plane.distance = dot(plane.normal, p0);

    return plane;
}

#define TILED_LIGHTING_TILE_CORNERS_COUNT 4

struct TiledLighting_TileBounds
{
    // top-left, top-right, bottom-left, bottom-right
    float3 frustumCorners[TILED_LIGHTING_TILE_CORNERS_COUNT];
    float3 frustumDirections[TILED_LIGHTING_TILE_CORNERS_COUNT];
};

struct TiledLighting_Aabb
{
    float3 center;
    float3 extents;
};

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
    const float2 texCoord = screenCoordinates.xy / _TiledLighting_ScreenDimensions;

    // Convert to clip space
    const float4 positionCs = float4(texCoord * 2.0f - 1.0f, screenCoordinates.z, screenCoordinates.w);
    return TiledLighting_ClipToView(positionCs);
}

uint TiledLighting_GetFlatTileIndex(const uint tileX, const uint tileY)
{
    return tileY * _TiledLighting_TilesX + tileX;
}

#include "../../ShaderLibrary/DepthNormals.hlsl"

struct TiledLighting_Sphere
{
    float3 center;
    float radius;
};

bool TiledLighting_SphereInsideAabb(const TiledLighting_Sphere sphere, const TiledLighting_Aabb aabb)
{
    const float3 delta = max(0, abs(aabb.center - sphere.center) - aabb.extents);
    const float distSq = dot(delta, delta);
    return distSq <= sphere.radius * sphere.radius;
}

uint TiledLighting_GetOpaqueLightGridIndex(const uint tileIndex)
{
    return tileIndex;
}

uint TiledLighting_GetOpaqueLightIndexListIndex(const uint tileIndex)
{
    return tileIndex + LIGHT_INDEX_LIST_BASE_INDEX_OFFSET;
}

#endif // TOON_RP_TILED_LIGHTING_SHARED