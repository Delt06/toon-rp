using UnityEngine;

namespace ToonRP.Runtime.Shadows
{
    public struct Bounds2D
    {
        public Vector2 Min, Max;

        public Bounds2D(Vector2 center, Vector2 size)
        {
            Vector2 extents = size * 0.5f;
            Min = center - extents;
            Max = center + extents;
        }

        public Vector2 Size => Max - Min;

        public Bounds AsXZ(float yCenter, float yExtents)
        {
            Vector3 min, max;
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

        public void Encapsulate(Vector2 point)
        {
            Min = Vector2.Min(Min, point);
            Max = Vector2.Max(Max, point);
        }
    }
}