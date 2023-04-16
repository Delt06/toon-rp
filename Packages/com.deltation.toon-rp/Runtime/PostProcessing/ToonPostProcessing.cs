using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing
{
    public class ToonPostProcessing
    {
        private static readonly int PostProcessingBufferId = Shader.PropertyToID("_ToonRP_PostProcessing");
        private ToonBloom _bloom;
        private Camera _camera;
        private ToonCameraRendererSettings _cameraRendererSettings;
        private RenderTextureFormat _colorFormat;
        private ScriptableRenderContext _context;
        private int _rtHeight;
        private int _rtWidth;
        private ToonPostProcessingSettings _settings;

        public bool HasFullScreenEffects =>
            _settings.HasFullScreenEffects() && _camera.cameraType <= CameraType.SceneView;

        public void Setup(in ScriptableRenderContext context, in ToonPostProcessingSettings settings,
            in ToonCameraRendererSettings cameraRendererSettings,
            RenderTextureFormat colorFormat, Camera camera, int rtWidth, int rtHeight)
        {
            _colorFormat = colorFormat;
            _context = context;
            _settings = settings;
            _cameraRendererSettings = cameraRendererSettings;
            _camera = camera;
            _rtWidth = rtWidth;
            _rtHeight = rtHeight;

            SetupBloom();
        }

        private void SetupBloom()
        {
            if (!_settings.Bloom.Enabled)
            {
                return;
            }

            _bloom ??= new ToonBloom();
            _bloom.Setup(_settings.Bloom, _colorFormat, _rtWidth, _rtHeight);
        }

        public void RenderFullScreenEffects(int width, int height, RenderTextureFormat format, int sourceId,
            RenderTargetIdentifier destination)
        {
            if (!HasFullScreenEffects)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.PostProcessing)))
            {
                RenderTargetIdentifier currentBuffer = sourceId;

                cmd.GetTemporaryRT(PostProcessingBufferId, width, height, 0,
                    _cameraRendererSettings.RenderTextureFilterMode, format,
                    RenderTextureReadWrite.Linear
                );

                if (_settings.Bloom.Enabled)
                {
                    _bloom.Render(cmd, sourceId, PostProcessingBufferId);
                    currentBuffer = PostProcessingBufferId;
                }

                if (currentBuffer != destination)
                {
                    using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.BlitPostProcessingResults)
                           ))
                    {
                        cmd.Blit(currentBuffer, destination);
                    }
                }
            }

            cmd.ReleaseTemporaryRT(PostProcessingBufferId);

            _context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}