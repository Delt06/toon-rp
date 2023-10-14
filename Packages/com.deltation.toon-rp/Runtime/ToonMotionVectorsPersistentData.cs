using UnityEngine;

namespace DELTation.ToonRP
{
    public sealed class ToonMotionVectorsPersistentData
    {
        private float _prevAspectRatio = -1;

        public int LastFrameIndex { get; private set; } = -1;

        public Matrix4x4 ViewProjection { get; private set; } = Matrix4x4.identity;

        internal Matrix4x4 PreviousViewProjection { get; private set; } = Matrix4x4.identity;

        public Matrix4x4 JitterMatrix { get; set; }

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
}