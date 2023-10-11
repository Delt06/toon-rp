using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class ToonAdditionalCameraData : MonoBehaviour, IAdditionalData
    {
        public ToonMotionVectorsPersistentData MotionVectorsPersistentData { get; } = new();
    }

    public sealed class ToonMotionVectorsPersistentData
    {
        private float _prevAspectRatio = -1;

        public int LastFrameIndex { get; private set; } = -1;

        public Matrix4x4 ViewProjection { get; private set; } = Matrix4x4.identity;

        internal Matrix4x4 PreviousViewProjection { get; private set; } = Matrix4x4.identity;

        public void Update(in ToonCameraData cameraData)
        {
            bool aspectChanged = !Mathf.Approximately(_prevAspectRatio, cameraData.AspectRatio);
            bool shouldUpdate = LastFrameIndex != Time.frameCount || aspectChanged;
            if (!shouldUpdate)
            {
                return;
            }

            Matrix4x4 gpuViewProjection = GL.GetGPUProjectionMatrix(cameraData.ProjectionMatrix, true) *
                                          cameraData.ViewMatrix;
            PreviousViewProjection = aspectChanged ? gpuViewProjection : ViewProjection;
            ViewProjection = gpuViewProjection;

            LastFrameIndex = Time.frameCount;
            _prevAspectRatio = cameraData.AspectRatio;
        }
    }

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