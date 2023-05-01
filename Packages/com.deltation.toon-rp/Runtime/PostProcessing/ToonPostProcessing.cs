using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing
{
    public class ToonPostProcessing
    {
        private static readonly int PostProcessingBufferId = Shader.PropertyToID("_ToonRP_PostProcessing");
        private List<IToonPostProcessingPass> _allFullScreenPasses;
        private Camera _camera;
        private ToonCameraRendererSettings _cameraRendererSettings;
        private RenderTextureFormat _colorFormat;
        private ScriptableRenderContext _context;
        private List<IToonPostProcessingPass> _enabledFullScreenPasses;
        private int _rtHeight;
        private int _rtWidth;
        private ToonPostProcessingSettings _settings;

        public bool AnyFullScreenEffectsEnabled => _enabledFullScreenPasses.Count > 0;

        public void UpdatePasses(Camera camera, in ToonPostProcessingSettings settings)
        {
            _enabledFullScreenPasses ??= new List<IToonPostProcessingPass>();
            _enabledFullScreenPasses.Clear();

            if (camera.cameraType > CameraType.SceneView)
            {
                return;
            }

            if (!settings.Enabled)
            {
                return;
            }

            _allFullScreenPasses ??= new List<IToonPostProcessingPass>
            {
                new ToonScreenSpaceOutline(),
                new ToonBloom(),
                new ToonFxaa(),
            };

            foreach (IToonPostProcessingPass pass in _allFullScreenPasses)
            {
                if (pass.IsEnabled(_settings))
                {
                    _enabledFullScreenPasses.Add(pass);
                }
            }
        }

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

            SetupPasses();
        }

        private void SetupPasses()
        {
            if (!AnyFullScreenEffectsEnabled)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.PostProcessing)))
            {
                var context = new ToonPostProcessingContext
                {
                    Settings = _settings,
                    ColorFormat = _colorFormat,
                    RtWidth = _rtWidth,
                    RtHeight = _rtHeight,
                    Camera = _camera,
                };

                foreach (IToonPostProcessingPass pass in _allFullScreenPasses)
                {
                    pass.Setup(cmd, context);
                }
            }

            _context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        public void RenderFullScreenEffects(int width, int height, RenderTextureFormat format, int sourceId,
            RenderTargetIdentifier destination)
        {
            if (!AnyFullScreenEffectsEnabled)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.PostProcessing)))
            {
                RenderTargetIdentifier currentSource = sourceId;
                RenderTargetIdentifier currentDestination = PostProcessingBufferId;

                cmd.GetTemporaryRT(PostProcessingBufferId, width, height, 0,
                    _cameraRendererSettings.RenderTextureFilterMode, format,
                    RenderTextureReadWrite.Linear
                );

                foreach (IToonPostProcessingPass pass in _enabledFullScreenPasses)
                {
                    pass.Render(cmd, currentSource, currentDestination);
                    (currentSource, currentDestination) = (currentDestination, currentSource);
                }

                if (currentSource != destination)
                {
                    using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.BlitPostProcessingResults)
                           ))
                    {
                        cmd.Blit(currentSource, destination);
                    }
                }
            }

            cmd.ReleaseTemporaryRT(PostProcessingBufferId);

            _context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        public void Cleanup()
        {
            if (!AnyFullScreenEffectsEnabled)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.PostProcessing)))
            {
                foreach (IToonPostProcessingPass pass in _enabledFullScreenPasses)
                {
                    pass.Cleanup(cmd);
                }
            }

            _context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}