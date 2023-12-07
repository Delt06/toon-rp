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
        private readonly ToonCameraRenderTarget _cameraRenderTarget;
        private readonly PrePassMode? _prePassMode;

        public ToonCameraOverride(Camera camera, ToonAdditionalCameraData additionalCameraData,
            ToonCameraRenderTarget cameraRenderTarget, PrePassMode? prePassMode = null, bool setInverse = false)
        {
            _prePassMode = prePassMode;
            _camera = camera;
            _additionalCameraData = additionalCameraData;
            _cameraRenderTarget = cameraRenderTarget;
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
            bool renderToTexture = _prePassMode != null ||
                                   _cameraRenderTarget.RenderToTexture ||
                                   _camera.targetTexture != null;
            Matrix4x4 projectionMatrix =
                ToonRpUtils.GetGPUProjectionMatrix(matrix, renderToTexture);
            ToonRpUtils.SetViewAndProjectionMatrices(cmd, _camera.worldToCameraMatrix, projectionMatrix,
                _setInverse
            );
            _overriden = true;
        }

        public void Restore(CommandBuffer cmd)
        {
            if (!_overriden)
            {
                return;
            }

            ToonRpUtils.SetViewAndProjectionMatrices(cmd,
                _camera.worldToCameraMatrix, _additionalCameraData.JitteredGpuProjectionMatrix,
                _setInverse
            );
            _overriden = false;
        }
    }
}