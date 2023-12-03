using JetBrains.Annotations;
using UnityEngine;

namespace DELTation.ToonRP.Shadows.Blobs
{
    internal static class FrustumPlaneProjectionUtils
    {
        private static readonly Vector3[] ViewportCorners =
        {
            new(0.0f, 0.0f, 0.0f),
            new(1.0f, 0.0f, 0.0f),
            new(0.0f, 1.0f, 0.0f),
            new(1.0f, 1.0f, 0.0f),
        };

        [MustUseReturnValue]
        public static Bounds2D? ComputeFrustumPlaneIntersection(Camera camera, float maxDistance, float receiverPlaneY)
        {
            maxDistance = Mathf.Min(maxDistance, camera.farClipPlane);

            var plane = new Plane(Vector3.up, receiverPlaneY);
            var context = new Context(maxDistance, plane);

            foreach (Vector3 corner in ViewportCorners)
            {
                ProcessRay(ref context, camera.ViewportPointToRay(corner));
                ProcessRay(ref context, camera.ViewportPointToRay(corner));
                ProcessRay(ref context, camera.ViewportPointToRay(corner));
                ProcessRay(ref context, camera.ViewportPointToRay(corner));
            }

            if (context.ExactHitsCount < 2)
            {
                return null;
            }

            return Bounds2D.FromMinMax(context.BoundsMin, context.BoundsMax);
        }

        private static void ProcessRay(ref Context context, in Ray ray)
        {
            float? t = IntersectPlane(context.Plane, ray) ?? IntersectPlane(context.PlaneFlipped, ray);

            bool exactHit = t <= context.MaxDistance;
            if (exactHit)
            {
                context.ExactHitsCount++;
            }

            Vector3 intersection = exactHit ? ray.GetPoint(t.Value) : ray.GetPoint(context.MaxDistance);
            intersection = context.Plane.ClosestPointOnPlane(intersection);
            var intersection2D = new Vector2(intersection.x, intersection.z);

            context.BoundsMin = Vector2.Min(context.BoundsMin, intersection2D);
            context.BoundsMax = Vector2.Max(context.BoundsMax, intersection2D);
        }

        private static float? IntersectPlane(Plane plane, Ray ray)
        {
            Vector3 point0 = plane.ClosestPointOnPlane(Vector3.zero);
            float denominator = Vector3.Dot(plane.normal, ray.direction);
            if (denominator > 1e-6)
            {
                Vector3 planeToRay = point0 - ray.origin;
                float t = Vector3.Dot(planeToRay, plane.normal) / denominator;
                return t >= 0 ? t : null;
            }

            return null;
        }

        private struct Context
        {
            public readonly float MaxDistance;
            public readonly Plane Plane;
            public readonly Plane PlaneFlipped;
            public Vector2 BoundsMin;
            public Vector2 BoundsMax;
            public int ExactHitsCount;

            public Context(float maxDistance, Plane plane)
            {
                MaxDistance = maxDistance;
                Plane = plane;
                PlaneFlipped = plane.flipped;
                BoundsMin = Vector2.positiveInfinity;
                BoundsMax = Vector2.negativeInfinity;
                ExactHitsCount = 0;
            }
        }
    }
}