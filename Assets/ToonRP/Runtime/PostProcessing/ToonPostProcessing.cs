using UnityEngine;
using UnityEngine.Rendering;

namespace ToonRP.Runtime.PostProcessing
{
    public class ToonPostProcessing
    {
        private const string BufferName = "Post-Processing";

        private readonly CommandBuffer _cmd = new() { name = BufferName };
        private ToonBloom _bloom;
        private Camera _camera;
        private RenderTextureFormat _colorFormat;
        private ScriptableRenderContext _context;
        private ToonPostProcessingSettings _settings;

        public bool IsActive => _settings.Enabled && _camera.cameraType <= CameraType.SceneView;

        public void Setup(in ScriptableRenderContext context, in ToonPostProcessingSettings settings,
            RenderTextureFormat colorFormat, Camera camera)
        {
            _colorFormat = colorFormat;
            _context = context;
            _settings = settings;
            _camera = camera;

            SetupBloom();
        }

        private void SetupBloom()
        {
            if (!_settings.Bloom.Enabled)
            {
                return;
            }

            _bloom ??= new ToonBloom();
            _bloom.Setup(_settings.Bloom, _colorFormat, _camera);
        }

        public void Render(int sourceId, RenderTargetIdentifier destination)
        {
            if (!IsActive)
            {
                return;
            }

            RenderTargetIdentifier currentBuffer = sourceId;

            if (_settings.Bloom.Enabled)
            {
                _bloom.Render(_cmd, sourceId, destination);
                currentBuffer = destination;
            }

            if (currentBuffer != destination)
            {
                const string sampleName = "Blit Post-Processing Result";
                _cmd.BeginSample(sampleName);
                _cmd.Blit(currentBuffer, destination);
                _cmd.EndSample(sampleName);
            }

            _context.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }
    }
}