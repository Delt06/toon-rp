using System.Runtime.CompilerServices;
using Unity.Mathematics;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Encapsulate(Bounds2D bounds)
        {
            Encapsulate(bounds.Min);
            Encapsulate(bounds.Max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
}