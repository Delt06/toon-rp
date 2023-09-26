// https://www.3dgep.com/forward-plus/
#define TILE_SIZE 16

#include "../../ShaderLibrary/Common.hlsl"

CBUFFER_START(TiledLighting)
    float2 _ScreenDimensions;
    uint _TilesX;
    uint _TilesY;
CBUFFER_END

#define COMPUTE_FRUSTUMS_GROUP_SIZE 16

struct Plane
{
    float3 normal;
    float distance;
};

Plane ComputePlane(const float3 p0, const float3 p1, const float3 p2)
{
    Plane plane;

    const float3 v0 = p1 - p0;
    const float3 v2 = p2 - p0;
    plane.normal = normalize(cross(v0, v2));

    // Compute the distance to the origin using p0.
    plane.distance = dot(plane.normal, p0);

    return plane;
}

struct Frustum
{
    Plane planes[4]; // left, right, top, bottom; back and front can be computed from depth values
};

RWStructuredBuffer<Frustum> _Frustums;

// Convert clip space coordinates to view space
float4 ClipToView(const float4 clip)
{
    // View space position.
    float4 view = mul(UNITY_MATRIX_I_P, clip);
    // Perspective projection.
    view = view / view.w;

    return view;
}

float4 ScreenToView(const float4 screenCoordinates)
{
    // Convert to normalized texture coordinates
    float2 texCoord = screenCoordinates.xy / _ScreenDimensions;

    #ifdef UNITY_UV_STARTS_AT_TOP
    texCoord.y = 1.0f - texCoord.y;
    #endif // UNITY_UV_STARTS_AT_TOP

    // Convert to clip space

    const float4 positionCs = float4(float2(texCoord.x, 1.0f - texCoord.y) * 2.0f - 1.0f, screenCoordinates.z,
                                     screenCoordinates.w);
    return ClipToView(positionCs);
}

uint GetFlatTileIndex(const uint tileX, const uint tileY)
{
    return tileY * _TilesX + tileX;
}

[numthreads(COMPUTE_FRUSTUMS_GROUP_SIZE, COMPUTE_FRUSTUMS_GROUP_SIZE, 1)]
void CS_ComputeFrustums(uint3 dispatchThreadId : SV_DispatchThreadID)
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
        viewSpace[i] = ScreenToView(screenSpace[i]).xyz;
    }

    const float3 eyePos = float3(0, 0, 0);
    Frustum frustum;
    // Left plane
    frustum.planes[0] = ComputePlane(eyePos, viewSpace[2], viewSpace[0]);
    // Right plane
    frustum.planes[1] = ComputePlane(eyePos, viewSpace[1], viewSpace[3]);
    // Top plane
    frustum.planes[2] = ComputePlane(eyePos, viewSpace[0], viewSpace[1]);
    // Bottom plane
    frustum.planes[3] = ComputePlane(eyePos, viewSpace[3], viewSpace[2]);

    if (tileCoords.x < _TilesX && tileCoords.y < _TilesY)
    {
        const uint frustumIndex = GetFlatTileIndex(tileCoords.x, tileCoords.y);
        _Frustums[frustumIndex] = frustum;
    }
}