using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing
{
    public class ToonPostProcessing : IDisposable
    {
        private static readonly int PostProcessingBufferId = Shader.PropertyToID("_ToonRP_PostProcessing");
        private List<IToonPostProcessingPass> _allFullScreenPasses;
        private ToonCameraRendererSettings _cameraRendererSettings;
        private ScriptableRenderContext _context;
        private List<IToonPostProcessingPass> _enabledFullScreenPasses;
        private ToonPostProcessingContext _postProcessingContext;

        public bool AnyFullScreenEffectsEnabled => _enabledFullScreenPasses.Count > 0;

        public void Dispose()
        {
            foreach (IToonPostProcessingPass pass in _allFullScreenPasses)
            {
                pass.Dispose();
            }
        }

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

            if (_allFullScreenPasses == null)
            {
                _allFullScreenPasses = new List<IToonPostProcessingPass>();

                if (settings.Passes != null)
                {
                    foreach (ToonPostProcessingPassAsset passAsset in settings.Passes
                                 .Where(p => p != null)
                                 .OrderBy(p => p.Order())
                            )
                    {
                        IToonPostProcessingPass pass = passAsset.CreatePass();
                        _allFullScreenPasses.Add(pass);
                    }
                }
            }

            foreach (IToonPostProcessingPass pass in _allFullScreenPasses)
            {
                if (pass.IsEnabled(settings))
                {
                    _enabledFullScreenPasses.Add(pass);
                }
            }
        }

        public void Setup(in ScriptableRenderContext context, in ToonPostProcessingSettings settings,
            in ToonCameraRendererSettings cameraRendererSettings,
            ToonAdditionalCameraData additionalCameraData,
            GraphicsFormat colorFormat, Camera camera, int rtWidth, int rtHeight)
        {
            _context = context;
            _cameraRendererSettings = cameraRendererSettings;

            _postProcessingContext = new ToonPostProcessingContext
            {
                Settings = settings,
                ColorFormat = colorFormat,
                RtWidth = rtWidth,
                RtHeight = rtHeight,
                Camera = camera,
                CameraRendererSettings = _cameraRendererSettings,
                AdditionalCameraData = additionalCameraData,
            };

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
                foreach (IToonPostProcessingPass pass in _allFullScreenPasses)
                {
                    pass.Setup(cmd, _postProcessingContext);
                }
            }

            _context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        public void RenderFullScreenEffects(int width, int height, GraphicsFormat format, int sourceId,
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
                    _cameraRendererSettings.RenderTextureFilterMode, format
                );

                foreach (IToonPostProcessingPass pass in _enabledFullScreenPasses)
                {
                    if (pass.NeedsDistinctSourceAndDestination())
                    {
                        pass.Render(cmd, currentSource, currentDestination);
                        (currentSource, currentDestination) = (currentDestination, currentSource);
                    }
                    else
                    {
                        pass.Render(cmd, currentSource, currentSource);
                    }
                }

                if (currentSource != destination)
                {
                    using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.BlitPostProcessingResults)
                           ))
                    {
                        cmd.SetRenderTarget(destination);
                        cmd.SetViewport(_postProcessingContext.Camera.pixelRect);
                        ToonBlitter.BlitDefault(cmd, currentSource);
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