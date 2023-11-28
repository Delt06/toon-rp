#ifndef TOON_RP_TILED_LIGHTING_SHARED
#define TOON_RP_TILED_LIGHTING_SHARED

// https://www.3dgep.com/forward-plus/

#define TILE_SIZE 16

// the opaque and transparent light counters are stored in the beginning of the buffer
#define LIGHT_INDEX_LIST_BASE_INDEX_OFFSET 2

#include "../../ShaderLibrary/Common.hlsl"
#include "../../ShaderLibrary/Lighting.hlsl"

CBUFFER_START(TiledLighting)
    float2 _TiledLighting_ScreenDimensions;
    uint _TiledLighting_TilesX;
    uint _TiledLighting_TilesY;

    uint _TiledLighting_CurrentLightIndexListOffset;
    uint _TiledLighting_CurrentLightGridOffset;

    uint _TiledLighting_ReservedLightsPerTile;
CBUFFER_END

struct TiledLight
{
    float4 color; // rgb = color
    float4 positionVs_range; // xyz = position VS, w = range
    float4 positionWs_attenuation; // xyz = position, w = 1/range^2
};

StructuredBuffer<TiledLight> _TiledLighting_Lights;

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

struct TiledLighting_Frustum
{
    TiledLighting_Plane planes[4]; // left, right, top, bottom; back and front can be computed from depth values
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

bool TiledLighting_SphereInsidePlane(const TiledLighting_Sphere sphere, const TiledLighting_Plane plane)
{
    return (dot(plane.normal, sphere.center) - plane.distance) < -sphere.radius;
}

bool TiledLighting_SphereInsideFrustum(const TiledLighting_Sphere sphere, const TiledLighting_Frustum frustum,
                                       const float zNear, const float zFar)
{
    bool result = true;

    if (sphere.center.z - sphere.radius > zNear || sphere.center.z + sphere.radius < zFar)
    {
        result = false;
    }

    // Then check frustum planes
    for (uint i = 0; i < 4 && result; i++)
    {
        if (TiledLighting_SphereInsidePlane(sphere, frustum.planes[i]))
        {
            result = false;
        }
    }

    return result;
}

uint TiledLighting_GetOpaqueLightGridIndex(const uint tileIndex)
{
    return tileIndex;
}

uint TiledLighting_GetTransparentLightGridIndex(const uint tileIndex)
{
    const uint transparentOffset = _TiledLighting_TilesX * _TiledLighting_TilesY;
    return transparentOffset + TiledLighting_GetOpaqueLightGridIndex(tileIndex);
}

uint TiledLighting_GetOpaqueLightIndexListIndex(const uint tileIndex)
{
    return tileIndex + LIGHT_INDEX_LIST_BASE_INDEX_OFFSET;
}

uint TiledLighting_GetTransparentLightIndexListIndex(const uint tileIndex)
{
    const uint transparentOffset = _TiledLighting_TilesX * _TiledLighting_TilesY * _TiledLighting_ReservedLightsPerTile;
    return transparentOffset + TiledLighting_GetOpaqueLightIndexListIndex(tileIndex);
}

#endif // TOON_RP_TILED_LIGHTING_SHARED