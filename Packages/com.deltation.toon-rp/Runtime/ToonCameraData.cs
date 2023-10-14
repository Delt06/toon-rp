using UnityEngine;

namespace DELTation.ToonRP
{
    public readonly struct ToonCameraData
    {
        public readonly Camera Camera;
        public readonly float AspectRatio;
        public readonly Matrix4x4 ViewMatrix;
        public readonly Matrix4x4 ProjectionMatrix;

        public ToonCameraData(Camera camera)
        {
            Camera = camera;
            AspectRatio = camera.aspect;
            ViewMatrix = camera.worldToCameraMatrix;
            ProjectionMatrix = camera.projectionMatrix;
        }
    }
}