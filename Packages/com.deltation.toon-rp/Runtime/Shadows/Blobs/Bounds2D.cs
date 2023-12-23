using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace DELTation.ToonRP.Shadows.Blobs
{
    [Serializable]
    public struct Bounds2D
    {
        public float2 Min, Max;

        private Bounds2D(float2 min, float2 max)
        {
            Min = min;
            Max = max;
        }

        public float2 Size
        {
            get => Max - Min;
            set
            {
                float2 center = (Min + Max) * 0.5f;
                float2 extents = value * 0.5f;
                Min = center - extents;
                Max = center + extents;
            }
        }

        public static Bounds2D FromMinMax(float2 min, float2 max) => new(min, max);

        public static Bounds2D FromCenterExtents(float2 center, float2 extents) =>
            new(center - extents, center + extents);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Encapsulate(in Bounds2D bounds)
        {
            Encapsulate(bounds.Min);
            Encapsulate(bounds.Max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public void Encapsulate(float2 point)
        {
            if (point.x < Min.x)
            {
                Min.x = point.x;
            }
            else if (point.x > Max.x)
            {
                Max.x = point.x;
            }

            if (point.y < Min.y)
            {
                Min.y = point.y;
            }
            else if (point.y > Max.y)
            {
                Max.y = point.y;
            }
        }
    }

    public static class Bounds2DExtensions
    {
        private const float IntersectionSizeTolerance = 0.001f;

        [BurstCompile]
        public static bool Intersects(this in Bounds2D bounds, in Bounds2D otherBounds)
        {
            float2 intersectionMin = max(bounds.Min, otherBounds.Min);
            float2 intersectionMax = min(bounds.Max, otherBounds.Max);
            return all(intersectionMax - intersectionMin >= IntersectionSizeTolerance);
        }
    }
}