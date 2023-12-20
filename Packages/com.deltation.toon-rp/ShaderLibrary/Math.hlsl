#ifndef TOON_RP_MATH
#define TOON_RP_MATH

float InverseLerpUnclamped(const float a, const float b, const float v)
{
    #pragma warning (disable : 4008) // Suppress the division by zero warning
    return (v - a) / (b - a);
    #pragma warning (restore : 4008)
}

float InverseLerpClamped(const float a, const float b, const float v)
{
    return saturate(InverseLerpUnclamped(a, b, v));
}

float InverseLerpClampedFast(const float a, const float invBMinusA, const float v)
{
    return saturate((v - a) * invBMinusA);
}

float SlopeOffsetFast(const float slope, const float offset, const float value)
{
    return saturate(slope * value + offset);
}

// https://www.ronja-tutorials.com/post/046-fwidth/
float StepAntiAliased(const float edge, const float value)
{
    const float halfChange = fwidth(value) * 0.5f;
    return InverseLerpClamped(edge - halfChange, edge + halfChange, value);
}

float DistanceFadeBase(const float distance, const float scale, const float fade)
{
    return saturate((1.0 - distance * scale) * fade);
}

float DistanceFade(const float distance, const float scale, const float fade)
{
    return 1.0f - DistanceFadeBase(distance, scale, fade);
}

float DistanceSquared(const float3 point1, const float3 point2)
{
    return dot(point1 - point2, point1 - point2);
}

float2 EncodePositionToUv(const float3 position, const float3 scale)
{
    return position.xy * scale.x +
        position.yz * scale.y +
        position.zx * scale.z;
}

#endif // TOON_RP_MATH