using System;
using System.Collections.Generic;
using System.Linq;
using DELTation.ToonRP.PostProcessing.BuiltIn;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing
{
    public class ToonPostProcessing : IDisposable
    {
        private static readonly int PostProcessingBufferId = Shader.PropertyToID("_ToonRP_PostProcessing");
        private static readonly int PostProcessingBufferNative0Id =
            Shader.PropertyToID("_ToonRP_PostProcessing_Native0");
        private static readonly int PostProcessingBufferNative1Id =
            Shader.PropertyToID("_ToonRP_PostProcessing_Native1");
        private readonly Dictionary<ToonPostProcessingPassAsset, IToonPostProcessingPass> _assetToPass = new();
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

            // Invalidate of any of the orders have changed
            foreach ((ToonPostProcessingPassAsset passAsset, IToonPostProcessingPass pass) in _assetToPass)
            {
                if (passAsset != null && passAsset.Order() == pass.Order)
                {
                    continue;
                }

                _allFullScreenPasses = null;
                _assetToPass.Clear();
                break;
            }

            if (_allFullScreenPasses == null)
            {
                _allFullScreenPasses = new List<IToonPostProcessingPass>();
                _assetToPass.Clear();

                if (settings.Passes != null)
                {
                    foreach ((ToonPostProcessingPassAsset passAsset, int order) in settings.Passes
                                 .Select(p => (Pass: p, Order: p.Order()))
                                 .Where(i => i.Pass != null)
                                 .OrderBy(i => i.Order)
                            )
                    {
                        IToonPostProcessingPass pass = passAsset.CreatePass();
                        pass.Order = order;
                        _allFullScreenPasses.Add(pass);
                        _assetToPass[passAsset] = pass;
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

                bool native = false;

                foreach (IToonPostProcessingPass pass in _enabledFullScreenPasses)
                {
                    bool switchedToNative = false;

                    if (pass.Order >= ToonPostProcessingPassOrders.SwitchToNativeResolution && !native)
                    {
                        int nativeWidth = _postProcessingContext.Camera.pixelWidth;
                        int nativeHeight = _postProcessingContext.Camera.pixelHeight;
                        cmd.GetTemporaryRT(PostProcessingBufferNative0Id, nativeWidth, nativeHeight, 0,
                            FilterMode.Point, RenderTextureFormat.Default
                        );
                        cmd.GetTemporaryRT(PostProcessingBufferNative1Id, nativeWidth, nativeHeight, 0,
                            FilterMode.Point, RenderTextureFormat.Default
                        );
                        currentDestination = PostProcessingBufferNative0Id;
                        native = true;
                        switchedToNative = true;
                    }

                    if (switchedToNative || pass.NeedsDistinctSourceAndDestination())
                    {
                        pass.Render(cmd, currentSource, currentDestination);

                        if (switchedToNative)
                        {
                            (currentSource, currentDestination) = (currentDestination, PostProcessingBufferNative1Id);
                        }
                        else
                        {
                            (currentSource, currentDestination) = (currentDestination, currentSource);
                        }
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

                cmd.ReleaseTemporaryRT(PostProcessingBufferId);

                if (native)
                {
                    cmd.ReleaseTemporaryRT(PostProcessingBufferNative0Id);
                    cmd.ReleaseTemporaryRT(PostProcessingBufferNative1Id);
                }
            }


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