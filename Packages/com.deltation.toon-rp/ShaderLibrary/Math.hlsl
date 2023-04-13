#ifndef TOON_RP_MATH
#define TOON_RP_MATH

float InverseLerpUnclamped(const float a, const float b, const float v)
{
    return (v - a) / (b - a);
}

float InverseLerpClamped(const float a, const float b, const float v)
{
    return saturate(InverseLerpUnclamped(a, b, v));
}

// https://www.ronja-tutorials.com/post/046-fwidth/
float StepAntiAliased(const float edge, const float value)
{
    const float halfChange = fwidth(value) * 0.5f;
    return InverseLerpClamped(edge - halfChange, edge + halfChange, value);
}

float DistanceFade(const float distance, const float scale, const float fade)
{
    return 1.0f - saturate((1.0 - distance * scale) * fade);
}

float DistanceSquared(const float3 point1, const float3 point2)
{
    return dot(point1 - point2, point1 - point2);
}

#endif // TOON_RP_MATH