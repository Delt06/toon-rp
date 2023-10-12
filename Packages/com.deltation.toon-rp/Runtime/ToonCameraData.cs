using UnityEngine;

namespace DELTation.ToonRP
{
    public readonly struct ToonCameraData
    {
        public readonly Camera Camera;
        public readonly float AspectRatio;
        public readonly Matrix4x4 ViewMatrix;
        public readonly Matrix4x4 ProjectionMatrix;
        public readonly Matrix4x4 JitteredProjectionMatrix;

        public ToonCameraData(Camera camera, Matrix4x4 jitteredProjectionMatrix)
        {
            Camera = camera;
            AspectRatio = camera.aspect;
            ViewMatrix = camera.worldToCameraMatrix;
            ProjectionMatrix = camera.projectionMatrix;
            JitteredProjectionMatrix = jitteredProjectionMatrix;
        }
    }
}