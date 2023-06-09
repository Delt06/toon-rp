#ifndef TOON_RP_FXAA_HIGH_QUALITY
#define TOON_RP_FXAA_HIGH_QUALITY

#include "ToonRPPostProcessingStackCommon.hlsl"

// https://catlikecoding.com/unity/tutorials/advanced-rendering/fxaa/

#define EDGE_SEARCH_STEPS 3
#define EDGE_SEARCH_STEP_SIZES 1.5, 2.0, 2.0
#define EDGE_SEARCH_LAST_STEP_GUESS 8.0

static const float EdgeStepSizes[EDGE_SEARCH_STEPS] = {EDGE_SEARCH_STEP_SIZES};

float GetSceneLuminance(float2 uv, float uOffset = 0.0, float vOffset = 0.0)
{
    uv += float2(uOffset, vOffset) * _MainTex_TexelSize.xy;
    return sqrt(Luminance(SampleSource(uv)));
}

struct LuminanceNeighborhood
{
    float center;
    float north;
    float east;
    float south;
    float west;

    float northEast;
    float southEast;
    float northWest;
    float southWest;


    float highest;
    float lowest;
    float range;
};

LuminanceNeighborhood GetLuminanceNeighborhood(float2 uv)
{
    LuminanceNeighborhood luminance;

    luminance.center = GetSceneLuminance(uv);
    luminance.north = GetSceneLuminance(uv, 0, 1);
    luminance.east = GetSceneLuminance(uv, 1, 0);
    luminance.south = GetSceneLuminance(uv, 0, -1);
    luminance.west = GetSceneLuminance(uv, -1, 0);

    luminance.northEast = GetSceneLuminance(uv, 1, 1);
    luminance.southEast = GetSceneLuminance(uv, 1, -1);
    luminance.northWest = GetSceneLuminance(uv, -1, 1);
    luminance.southWest = GetSceneLuminance(uv, -1, -1);

    luminance.highest = max(luminance.center,
                            max(luminance.north, max(luminance.east, max(luminance.south, luminance.west))));
    luminance.lowest = min(luminance.center,
                           min(luminance.north, min(luminance.east, min(luminance.south, luminance.west))));
    luminance.range = luminance.highest - luminance.lowest;

    return luminance;
}

bool CanSkipFxaa(const in LuminanceNeighborhood luminance)
{
    return luminance.range < max(_FXAA_FixedContrastThreshold, _FXAA_RelativeContrastThreshold * luminance.highest);
}

float GetSubpixelBlendFactor(in LuminanceNeighborhood luminance)
{
    float filter = 2.0 * (luminance.north + luminance.east + luminance.south + luminance.west);
    filter += luminance.northEast + luminance.northWest + luminance.southEast + luminance.southWest;
    filter *= 1.0f / 12.0f;
    filter = abs(filter - luminance.center);
    filter = saturate(filter / luminance.range);
    filter = smoothstep(0, 1, filter);
    return filter * filter * _FXAA_SubpixelBlendingFactor;
}

bool IsHorizontalEdge(const LuminanceNeighborhood luminance)
{
    const float horizontal =
        2.0 * abs(luminance.north + luminance.south - 2.0 * luminance.center) +
        abs(luminance.northEast + luminance.southEast - 2.0 * luminance.east) +
        abs(luminance.northWest + luminance.southWest - 2.0 * luminance.west);
    const float vertical =
        2.0 * abs(luminance.east + luminance.west - 2.0 * luminance.center) +
        abs(luminance.northEast + luminance.northWest - 2.0 * luminance.north) +
        abs(luminance.southEast + luminance.southWest - 2.0 * luminance.south);
    return horizontal >= vertical;
}

struct Edge
{
    bool isHorizontal;
    float pixelStep;
    float luminanceGradient, otherLuminance;
};

Edge GetEdge(const in LuminanceNeighborhood luminance)
{
    Edge edge;
    edge.isHorizontal = IsHorizontalEdge(luminance);

    float luminancePositive, luminanceNegative;
    if (edge.isHorizontal)
    {
        edge.pixelStep = _MainTex_TexelSize.y;
        luminancePositive = luminance.north;
        luminanceNegative = luminance.south;
    }
    else
    {
        edge.pixelStep = _MainTex_TexelSize.x;
        luminancePositive = luminance.east;
        luminanceNegative = luminance.west;
    }

    const float gradientPositive = abs(luminancePositive - luminance.center);
    const float gradientNegative = abs(luminanceNegative - luminance.center);

    if (gradientPositive < gradientNegative)
    {
        edge.pixelStep = -edge.pixelStep;
        edge.luminanceGradient = gradientNegative;
        edge.otherLuminance = luminanceNegative;
    }
    else
    {
        edge.luminanceGradient = gradientPositive;
        edge.otherLuminance = luminancePositive;
    }

    return edge;
}

float GetEdgeBlendFactor(in LuminanceNeighborhood luminance, in Edge edge, float2 uv)
{
    float2 edgeUV = uv;
    float2 uvStep = 0;

    if (edge.isHorizontal)
    {
        edgeUV.y += 0.5 * edge.pixelStep;
        uvStep.x = _MainTex_TexelSize.x;
    }
    else
    {
        edgeUV.x += 0.5 * edge.pixelStep;
        uvStep.y = _MainTex_TexelSize.y;
    }

    const float edgeLuminance = 0.5 * (luminance.center + edge.otherLuminance);
    const float gradientThreshold = 0.25 * edge.luminanceGradient;

    float2 uvPositive = edgeUV + uvStep;
    float lumaGradientPositive = GetSceneLuminance(uvPositive) - edgeLuminance;
    bool atEndPositive = abs(lumaGradientPositive) >= gradientThreshold;

    uint i;

    UNITY_UNROLL
    for (i = 0; i < EDGE_SEARCH_STEPS && !atEndPositive; ++i)
    {
        uvPositive += uvStep * EdgeStepSizes[i];
        lumaGradientPositive = GetSceneLuminance(uvPositive) - edgeLuminance;
        atEndPositive = abs(lumaGradientPositive) >= gradientThreshold;
    }

    if (!atEndPositive)
    {
        uvPositive += uvStep * EDGE_SEARCH_LAST_STEP_GUESS;
    }

    float2 uvNegative = edgeUV - uvStep;
    float lumaGradientNegative = GetSceneLuminance(uvNegative) - edgeLuminance;
    bool atEndNegative = abs(lumaGradientNegative) >= gradientThreshold;

    UNITY_UNROLL
    for (i = 0; i < EDGE_SEARCH_STEPS && !atEndNegative; ++i)
    {
        uvNegative -= uvStep * EdgeStepSizes[i];
        lumaGradientNegative = GetSceneLuminance(uvNegative) - edgeLuminance;
        atEndNegative = abs(lumaGradientNegative) >= gradientThreshold;
    }

    if (!atEndNegative)
    {
        uvNegative -= uvStep * EDGE_SEARCH_LAST_STEP_GUESS;
    }

    float distanceToEndPositive, distanceToEndNegative;
    if (edge.isHorizontal)
    {
        distanceToEndPositive = uvPositive.x - uv.x;
        distanceToEndNegative = uv.x - uvNegative.x;
    }
    else
    {
        distanceToEndPositive = uvPositive.y - uv.y;
        distanceToEndNegative = uv.y - uvNegative.y;
    }

    float distanceToNearestEnd;
    bool deltaSign;
    if (distanceToEndPositive <= distanceToEndNegative)
    {
        distanceToNearestEnd = distanceToEndPositive;
        deltaSign = lumaGradientPositive >= 0;
    }
    else
    {
        distanceToNearestEnd = distanceToEndNegative;
        deltaSign = lumaGradientNegative >= 0;
    }

    if (deltaSign == (luminance.center - edgeLuminance >= 0))
    {
        return 0.0;
    }

    return 0.5 - distanceToNearestEnd / (distanceToEndPositive + distanceToEndNegative);
}

float3 ApplyFxaa(const float2 uv)
{
    const LuminanceNeighborhood luminance = GetLuminanceNeighborhood(uv);

    UNITY_BRANCH
    if (CanSkipFxaa(luminance))
    {
        return SampleSource(uv);
    }

    const Edge edge = GetEdge(luminance);
    const float blendFactor = max(
        GetSubpixelBlendFactor(luminance),
        GetEdgeBlendFactor(luminance, edge, uv)
    );
    float2 blendUv = uv;
    if (edge.isHorizontal)
    {
        blendUv.y += blendFactor * edge.pixelStep;
    }
    else
    {
        blendUv.x += blendFactor * edge.pixelStep;
    }

    return SampleSource(blendUv);
}

#endif // TOON_RP_FXAA_HIGH_QUALITY