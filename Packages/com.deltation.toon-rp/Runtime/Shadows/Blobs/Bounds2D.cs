using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace DELTation.ToonRP.Shadows.Blobs
{
    public struct Bounds2D
    {
        public float2 Min, Max;

        public Bounds2D(float2 center, float2 size)
        {
            float2 extents = size * 0.5f;
            Min = center - extents;
            Max = center + extents;
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

        public bool IsEmpty => all(Size == 0.0f);

        public Bounds AsXZ(float yCenter, float yExtents)
        {
            float3 min, max;
            min.x = Min.x;
            min.y = yCenter - yExtents;
            min.z = Min.y;

            max.x = Max.x;
            max.y = yCenter + yExtents;
            max.z = Max.y;

            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        public void Encapsulate(Bounds2D bounds)
        {
            Encapsulate(bounds.Min);
            Encapsulate(bounds.Max);
        }

        public void Encapsulate(float2 point)
        {
            Min = min(Min, point);
            Max = max(Max, point);
        }
    }
}