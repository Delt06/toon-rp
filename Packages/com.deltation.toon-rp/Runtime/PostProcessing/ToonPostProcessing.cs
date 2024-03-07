using System;
using System.Collections.Generic;
using System.Linq;
using DELTation.ToonRP.PostProcessing.BuiltIn;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing
{
    public class ToonPostProcessing : IDisposable
    {
        public delegate bool PassPredicate([NotNull] IToonPostProcessingPass pass,
            in ToonPostProcessingContext context);

        private static readonly int PostProcessingBuffer0Id = Shader.PropertyToID("_ToonRP_PostProcessing_0");
        private static readonly int PostProcessingBuffer1Id = Shader.PropertyToID("_ToonRP_PostProcessing_1");
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
            if (_allFullScreenPasses != null)
            {
                foreach (IToonPostProcessingPass pass in _allFullScreenPasses)
                {
                    pass.Dispose();
                }
            }
        }

        public void PreSetup(Camera camera, in ToonPostProcessingSettings settings)
        {
            _enabledFullScreenPasses ??= new List<IToonPostProcessingPass>();
            _enabledFullScreenPasses.Clear();

            if (camera.cameraType > CameraType.SceneView || ToonSceneViewUtils.IsDrawingWireframes(camera))
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
                                 .Where(p => p != null)
                                 .Select(p => (Pass: p, Order: p.Order()))
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
            ToonCameraRenderTarget cameraRenderTarget,
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
                CameraRenderTarget = cameraRenderTarget,
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

        public void RenderFullScreenEffects(in ToonRenderPipelineSharedContext sharedContext,
            int width, int height, GraphicsFormat format,
            RenderTargetIdentifier sourceId,
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
                RenderTargetIdentifier currentDestination =
                    GetTemporaryRT(cmd, PostProcessingBuffer0Id, width, height,
                        _cameraRendererSettings.RenderTextureFilterMode, format
                    );
                RenderTargetIdentifier native0Id;
                RenderTargetIdentifier native1Id = default;

                bool native = false;

                for (int passIndex = 0; passIndex < _enabledFullScreenPasses.Count; passIndex++)
                {
                    IToonPostProcessingPass pass = _enabledFullScreenPasses[passIndex];
                    bool switchedToNative = false;

                    if (pass.Order >= ToonPostProcessingPassOrders.SwitchToNativeResolution && !native)
                    {
                        int nativeWidth = _postProcessingContext.Camera.pixelWidth;
                        int nativeHeight = _postProcessingContext.Camera.pixelHeight;
                        GraphicsFormat defaultGraphicsFormat =
                            GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default, true);
                        native0Id = GetTemporaryRT(cmd, PostProcessingBufferNative0Id, nativeWidth, nativeHeight,
                            FilterMode.Point, defaultGraphicsFormat
                        );
                        native1Id = GetTemporaryRT(cmd, PostProcessingBufferNative1Id, nativeWidth, nativeHeight,
                            FilterMode.Point, defaultGraphicsFormat
                        );
                        currentDestination = native0Id;
                        native = true;
                        switchedToNative = true;
                    }

                    // Case 1: source and destination need to be distinct
                    if (switchedToNative || pass.NeedsDistinctSourceAndDestination() || currentSource == sourceId)
                    {
                        bool destinationIsIntermediateTexture = true;

                        if (switchedToNative && passIndex == _enabledFullScreenPasses.Count - 1)
                        {
                            currentDestination = destination;
                            destinationIsIntermediateTexture = _postProcessingContext.Camera.targetTexture != null;
                        }

                        pass.Render(cmd, currentSource, currentDestination, destinationIsIntermediateTexture);

                        if (currentSource == sourceId)
                        {
                            // Avoid reusing the original resource at it may cause repeated MSAA resolve
                            currentSource = GetTemporaryRT(cmd, PostProcessingBuffer1Id, width, height,
                                _cameraRendererSettings.RenderTextureFilterMode, format
                            );
                        }

                        if (switchedToNative)
                        {
                            (currentSource, currentDestination) = (currentDestination, native1Id);
                        }
                        else
                        {
                            (currentSource, currentDestination) = (currentDestination, currentSource);
                        }
                    }
                    // Case 2: source and destination can be the same
                    else
                    {
                        const bool destinationIsIntermediateTexture = true;
                        pass.Render(cmd, currentSource, currentSource, destinationIsIntermediateTexture);
                    }
                }

                if (currentSource != destination)
                {
                    using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.BlitPostProcessingResults)
                           ))
                    {
                        _postProcessingContext.CameraRenderTarget.FinalBlit(cmd, sharedContext, currentSource);
                    }
                }

                cmd.ReleaseTemporaryRT(PostProcessingBuffer0Id);
                cmd.ReleaseTemporaryRT(PostProcessingBuffer1Id);

                if (native)
                {
                    cmd.ReleaseTemporaryRT(PostProcessingBufferNative0Id);
                    cmd.ReleaseTemporaryRT(PostProcessingBufferNative1Id);
                }
            }


            _context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        private RenderTargetIdentifier GetTemporaryRT(CommandBuffer cmd, int id, int width, int height,
            FilterMode filterMode, GraphicsFormat format)
        {
            const int depthBuffer = 0;

#if ENABLE_VR && ENABLE_XR_MODULE
            XRPass xrPass = _postProcessingContext.AdditionalCameraData.XrPass;
            if (xrPass.enabled)
            {
                int arraySize = xrPass.viewCount;
                cmd.GetTemporaryRTArray(id, width, height, arraySize, depthBuffer, filterMode, format);
                return ToonRpUtils.FixupTextureArrayIdentifier(id);
            }
#endif // ENABLE_VR && ENABLE_XR_MODULE

            cmd.GetTemporaryRT(id, width, height, depthBuffer, filterMode, format);
            return id;
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

        public bool TrueForAny(PassPredicate predicate)
        {
            foreach (IToonPostProcessingPass pass in _enabledFullScreenPasses)
            {
                if (predicate(pass, _postProcessingContext))
                {
                    return true;
                }
            }

            return false;
        }
    }
}