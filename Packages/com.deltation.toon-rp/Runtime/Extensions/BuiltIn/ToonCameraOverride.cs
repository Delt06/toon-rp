using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    public struct ToonCameraOverride
    {
        private readonly float _aspectRatio;
        private readonly float _zNear;
        private readonly float _zFar;
        private readonly Camera _camera;
        private bool _overriden;
        private readonly ToonAdditionalCameraData _additionalCameraData;
        private readonly bool _setInverse;

        public ToonCameraOverride(Camera camera, ToonAdditionalCameraData additionalCameraData, bool setInverse = false)
        {
            _camera = camera;
            _additionalCameraData = additionalCameraData;
            Rect pixelRect = camera.pixelRect;
            _aspectRatio = pixelRect.width / pixelRect.height;
            _zNear = camera.nearClipPlane;
            _zFar = camera.farClipPlane;
            _overriden = false;
            _setInverse = setInverse;
        }

        public void OverrideIfEnabled(CommandBuffer cmd, in ToonCameraOverrideSettings settings)
        {
#if UNITY_EDITOR
            if (_camera.cameraType != CameraType.Game)
            {
                return;
            }
#endif // UNITY_EDITOR

            if (!settings.Enabled)
            {
                return;
            }

            float fieldOfView = settings.FieldOfView;
            var matrix = Matrix4x4.Perspective(fieldOfView, _aspectRatio,
                _zNear,
                _zFar
            );
            Matrix4x4 projectionMatrix = ToonRpUtils.GetGPUProjectionMatrixForCamera(matrix, _camera);
            ToonRpUtils.SetViewAndProjectionMatrices(cmd, _camera.worldToCameraMatrix, projectionMatrix, JitterMatrix,
                _setInverse
            );
            _overriden = true;
        }

        public void Restore(ref ScriptableRenderContext context)
        {
            if (!_overriden)
            {
                return;
            }

            ToonRpUtils.SetupCameraProperties(ref context, _camera,
                _additionalCameraData.JitteredProjectionMatrix
            );
            _overriden = false;
        }

        private Matrix4x4 JitterMatrix => _additionalCameraData.MotionVectorsPersistentData.JitterMatrix;
    }
}