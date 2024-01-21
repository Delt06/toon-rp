using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public sealed class ToonCameraRenderTarget
    {
        private static readonly int ToonScreenParamsId = Shader.PropertyToID("_ToonRP_ScreenParams");
        private static readonly int ScreenParamsId = Shader.PropertyToID("_ScreenParams");
        private static readonly int RenderToTextureColorBufferId = Shader.PropertyToID("_ToonRP_CameraColorBuffer");
        private static readonly int RenderToTextureDepthBufferId = Shader.PropertyToID("_ToonRP_CameraDepthBuffer");
        private static readonly int RenderToTextureMsColorBufferId =
            Shader.PropertyToID("_ToonRP_CameraColorBuffer_MSAA");
        private static readonly int RenderToTextureMsDepthBufferId =
            Shader.PropertyToID("_ToonRP_CameraDepthBuffer_MSAA");

        private Camera _camera;
        private FilterMode _filterMode;
        private bool _inRenderPass;

        private State _state;
        private bool _useNativeRenderPasses;
        public RenderTargetIdentifier CameraTargetColorId { get; private set; } = BuiltinRenderTextureType.CameraTarget;
        public RenderTargetIdentifier CameraTargetDepthId { get; private set; } = BuiltinRenderTextureType.CameraTarget;

        public bool ForceStoreAttachments { get; set; } = true;

        private int MsaaSamples { get; set; }
        public bool RenderToTexture { get; private set; }
        private GraphicsFormat DepthStencilFormat { get; set; }

        public GraphicsFormat ColorFormat { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }
        public Rect PixelRect { get; private set; }

        public void ConfigureNativeRenderPasses(bool useNativeRenderPasses)
        {
            _useNativeRenderPasses = useNativeRenderPasses && ToonGraphicsApi.SupportsNativeRenderPasses;
        }

        public RenderTargetIdentifier CurrentColorBufferId(bool shouldBeAllocated = true)
        {
            if (shouldBeAllocated)
            {
                Assert.IsTrue(_state.ColorBufferId.IsAllocated);
            }

            return _state.ColorBufferId.Identifier;
        }

        public void FinalBlit(CommandBuffer cmd)
        {
            if (RenderToTexture)
            {
                cmd.SetRenderTarget(CameraTargetColorId);

                var screenParams = new ScreenParams(_camera.pixelWidth, _camera.pixelHeight)
                {
                    CameraRect = _camera.rect,
                    SetViewportRect = false,
                };
                SetScreenParamsOverride(cmd, screenParams);
                cmd.SetViewport(_camera.pixelRect);

                ToonBlitter.BlitDefault(cmd, RenderToTextureColorBufferId);
            }
        }


        public void Initialize(Camera camera, bool renderToTexture, int width, int height, Rect pixelRect,
            FilterMode filterMode,
            GraphicsFormat colorFormat, GraphicsFormat depthStencilFormat, int msaaSamples)
        {
            _filterMode = filterMode;
            RenderToTexture = renderToTexture;
            _camera = camera;
            Width = width;
            Height = height;
            PixelRect = pixelRect;
            ColorFormat = colorFormat;
            MsaaSamples = msaaSamples;
            DepthStencilFormat = depthStencilFormat;
            _state = default;
        }

        public void SetCameraTarget(RenderTargetIdentifier colorId, RenderTargetIdentifier depthId)
        {
            CameraTargetColorId = colorId;
            CameraTargetDepthId = depthId;
        }

        public void InitState()
        {
            if (RenderToTexture)
            {
                var colorDesc = new RenderTextureDescriptor(Width, Height,
                    ColorFormat, 0, 1
                );
                var depthDesc = new RenderTextureDescriptor(Width, Height,
                    GraphicsFormat.None, DepthStencilFormat, 1
                );

                _state.ColorBufferId = CameraRtId.Temporary(RenderToTextureColorBufferId, colorDesc, _filterMode);

                if (ForceStoreAttachments || !_useNativeRenderPasses)
                {
                    _state.DepthBufferId =
                        CameraRtId.Temporary(RenderToTextureDepthBufferId, depthDesc, FilterMode.Point);
                }

                if (!_useNativeRenderPasses && MsaaSamples > 1)
                {
                    RenderTextureDescriptor msColorDesc = colorDesc;
                    msColorDesc.msaaSamples = MsaaSamples;
                    _state.MsColorBufferId =
                        CameraRtId.Temporary(RenderToTextureMsColorBufferId, msColorDesc, _filterMode);

                    RenderTextureDescriptor msDepthDesc = depthDesc;
                    msDepthDesc.msaaSamples = MsaaSamples;
                    _state.MsDepthBufferId =
                        CameraRtId.Temporary(RenderToTextureMsDepthBufferId, msDepthDesc, FilterMode.Point);
                }
            }
            else
            {
                _state.ColorBufferId = CameraRtId.Persistent(CameraTargetColorId);
                _state.DepthBufferId = CameraRtId.Persistent(CameraTargetDepthId);
            }
        }

        public void BeginRenderPass(ref ScriptableRenderContext context,
            RenderBufferLoadAction loadAction,
            in ToonClearValue clearValue = default)
        {
            Assert.IsFalse(_inRenderPass);

            CommandBuffer cmd = CommandBufferPool.Get();

            BeginRenderPass(ref context, cmd, loadAction, clearValue);

            SetScreenParams(cmd);
            context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
            _inRenderPass = true;
        }

        private void BeginRenderPass(ref ScriptableRenderContext context, CommandBuffer cmd,
            RenderBufferLoadAction loadAction,
            ToonClearValue clearValue)
        {
            int msaaSamples = _camera.allowMSAA ? MsaaSamples : 1;
            if (msaaSamples > 1 && (loadAction == RenderBufferLoadAction.Load ||
                                    ForceStoreAttachments && !ToonGraphicsApi.SupportsMultisampleDepthResolve()))
            {
                msaaSamples = 1;
            }

            if (_state.ColorBufferId.IsValid)
            {
                _state.ColorBufferId.EnsureTemporaryRT(cmd);
            }

            if (_state.DepthBufferId.IsValid)
            {
                _state.DepthBufferId.EnsureTemporaryRT(cmd);
            }

            context.ExecuteCommandBufferAndClear(cmd);

            if (_useNativeRenderPasses)
            {
                BeginNativeRenderPass(ref context, cmd, loadAction, clearValue, msaaSamples);
            }
            else
            {
                BeginFallbackRenderPass(ref context, cmd, loadAction, clearValue, msaaSamples);
            }
        }

        private void BeginNativeRenderPass(ref ScriptableRenderContext context, CommandBuffer cmd,
            RenderBufferLoadAction loadAction,
            ToonClearValue clearValue, int msaaSamples)
        {
            const int colorIndex = 0;
            const int depthIndex = 1;

            bool usingMsaa = msaaSamples > 1;

            {
                var attachments =
                    new NativeArray<AttachmentDescriptor>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory
                    );

                var colorAttachment = new AttachmentDescriptor(ColorFormat);
                RenderBufferStoreAction storeAction =
                    usingMsaa ? RenderBufferStoreAction.Resolve : RenderBufferStoreAction.Store;
                {
                    colorAttachment.loadAction = loadAction;
                    colorAttachment.storeAction = storeAction;

                    if (clearValue.ClearColor)
                    {
                        colorAttachment.ConfigureClear(clearValue.BackgroundColor);
                    }

                    if (colorAttachment.loadAction == RenderBufferLoadAction.Load ||
                        colorAttachment.storeAction == RenderBufferStoreAction.Store)
                    {
                        colorAttachment.loadStoreTarget = _state.ColorBufferId.Identifier;
                    }

                    if (colorAttachment.storeAction == RenderBufferStoreAction.Resolve)
                    {
                        colorAttachment.resolveTarget = _state.ColorBufferId.Identifier;
                    }
                }

                var depthAttachment = new AttachmentDescriptor(DepthStencilFormat);
                {
                    depthAttachment.loadAction = loadAction;
                    depthAttachment.storeAction =
                        ForceStoreAttachments ? storeAction : RenderBufferStoreAction.DontCare;

                    if (clearValue.ClearDepth)
                    {
                        depthAttachment.ConfigureClear(Color.black);
                    }

                    if (depthAttachment.loadAction == RenderBufferLoadAction.Load ||
                        depthAttachment.storeAction == RenderBufferStoreAction.Store)
                    {
                        depthAttachment.loadStoreTarget = _state.DepthBufferId.Identifier;
                    }

                    if (depthAttachment.storeAction == RenderBufferStoreAction.Resolve)
                    {
                        depthAttachment.resolveTarget = _state.DepthBufferId.Identifier;
                    }

                    // Even if we don't store/resolve depth, we have to set anyway to prevent flickering on orientation change
                    if (!usingMsaa &&
                        depthAttachment.storeAction == RenderBufferStoreAction.DontCare &&
                        _state.DepthBufferId.Identifier == BuiltinRenderTextureType.CameraTarget
                       )
                    {
                        depthAttachment.loadStoreTarget = _state.DepthBufferId.Identifier;
                    }

                    Assert.IsTrue(depthAttachment.resolveTarget == BuiltinRenderTextureType.None ||
                                  ToonGraphicsApi.SupportsMultisampleDepthResolve()
                    );

                    // Specifying camera depth more precisely is required here
                    if (depthAttachment.loadStoreTarget == BuiltinRenderTextureType.CameraTarget)
                    {
                        depthAttachment.loadStoreTarget = BuiltinRenderTextureType.Depth;
                    }

                    if (depthAttachment.resolveTarget == BuiltinRenderTextureType.CameraTarget)
                    {
                        depthAttachment.resolveTarget = BuiltinRenderTextureType.Depth;
                    }
                }

                attachments[colorIndex] = colorAttachment;
                attachments[depthIndex] = depthAttachment;

                int fullScreenWidth = Width, fullScreenHeight = Height;

                if (!RenderToTexture)
                {
                    Rect cameraRect = _camera.rect;
                    if (cameraRect != new Rect(0, 0, 1, 1))
                    {
                        Rect pixelRect = _camera.pixelRect;
                        fullScreenWidth = Mathf.RoundToInt(pixelRect.width / cameraRect.width);
                        fullScreenHeight = Mathf.RoundToInt(pixelRect.height / cameraRect.height);
                    }
                }

                context.BeginRenderPass(fullScreenWidth, fullScreenHeight, msaaSamples, attachments, depthIndex);
                attachments.Dispose();
            }

            {
                var colorIndices = new NativeArray<int>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                colorIndices[0] = colorIndex;
                context.BeginSubPass(colorIndices);
                colorIndices.Dispose();
            }

            SetViewport(cmd);
            context.ExecuteCommandBufferAndClear(cmd);
        }

        private void BeginFallbackRenderPass(ref ScriptableRenderContext context, CommandBuffer cmd,
            RenderBufferLoadAction loadAction,
            ToonClearValue clearValue, int msaaSamples)
        {
            RenderTargetIdentifier colorId, depthId;

            _state.ColorBufferId.EnsureTemporaryRT(cmd);
            _state.DepthBufferId.EnsureTemporaryRT(cmd);

            if (msaaSamples > 1)
            {
                _state.MsColorBufferId.EnsureTemporaryRT(cmd);
                _state.MsDepthBufferId.EnsureTemporaryRT(cmd);
                colorId = _state.MsColorBufferId.Identifier;
                depthId = _state.MsDepthBufferId.Identifier;
            }
            else
            {
                colorId = _state.ColorBufferId.Identifier;
                depthId = _state.DepthBufferId.Identifier;
            }

            if (loadAction == RenderBufferLoadAction.Clear)
            {
                loadAction = RenderBufferLoadAction.DontCare;
            }

            const RenderBufferStoreAction colorStoreAction = RenderBufferStoreAction.Store;
            RenderBufferStoreAction depthStoreAction =
                ForceStoreAttachments ? RenderBufferStoreAction.Store : RenderBufferStoreAction.DontCare;

            cmd.SetRenderTarget(
                colorId, loadAction, colorStoreAction,
                depthId, loadAction, depthStoreAction
            );

            SetViewport(cmd);

            if (clearValue.ClearColor || clearValue.ClearDepth)
            {
                cmd.ClearRenderTarget(clearValue.ClearDepth, clearValue.ClearColor, clearValue.BackgroundColor);
            }

            context.ExecuteCommandBufferAndClear(cmd);
        }

        private void SetViewport(CommandBuffer cmd)
        {
            // Rect pixelViewport;
            //
            // if (_additionalCameraData.XrPass.enabled)
            // {
            //     Rect viewport = RenderToTexture ? new Rect(0, 0, 1.0f, 1.0f) : _camera.rect;
            //     Rect xrViewport = _additionalCameraData.XrPass.GetViewport();
            //     pixelViewport = new Rect(viewport.x * xrViewport.width + xrViewport.x,
            //         viewport.y * xrViewport.height + xrViewport.y,
            //         viewport.width * xrViewport.width,
            //         viewport.height * xrViewport.height
            //     );
            // }
            // else
            // {
            //     pixelViewport = RenderToTexture ? new Rect(0, 0, Width, Height) : _camera.pixelRect;
            // }

            cmd.SetViewport(RenderToTexture ? new Rect(0, 0, Width, Height) : PixelRect);
        }

        public void EndRenderPass(ref ScriptableRenderContext context, CommandBuffer cmd)
        {
            Assert.IsTrue(_inRenderPass);

            if (_useNativeRenderPasses)
            {
                context.EndSubPass();
                context.EndRenderPass();
            }
            else
            {
                _state.ColorBufferId.EnsureTemporaryRT(cmd);
                _state.DepthBufferId.EnsureTemporaryRT(cmd);

                if (_state.MsColorBufferId.IsAllocated)
                {
                    using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Resolve Camera Color")))
                    {
                        Assert.IsTrue(_state.MsColorBufferId.IsValid);
                        Assert.IsTrue(_state.ColorBufferId.IsValid);
                        cmd.Blit(_state.MsColorBufferId.Identifier, _state.ColorBufferId.Identifier);
                    }

                    _state.MsColorBufferId.ReleaseTemporaryRT(cmd);
                }

                if (_state.MsDepthBufferId.IsAllocated && ForceStoreAttachments)
                {
                    using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Resolve Camera Depth")))
                    {
                        Assert.IsTrue(_state.MsDepthBufferId.IsValid);
                        Assert.IsTrue(_state.DepthBufferId.IsValid);
                        cmd.Blit(_state.MsDepthBufferId.Identifier, _state.DepthBufferId.Identifier);
                    }

                    _state.MsDepthBufferId.ReleaseTemporaryRT(cmd);
                }

                context.ExecuteCommandBufferAndClear(cmd);
            }

            _inRenderPass = false;
        }

        public void ReleaseTemporaryRTs(CommandBuffer cmd)
        {
            _state.ColorBufferId.ReleaseTemporaryRT(cmd);
            _state.DepthBufferId.ReleaseTemporaryRT(cmd);

            _state.MsColorBufferId.ReleaseTemporaryRT(cmd);
            _state.MsDepthBufferId.ReleaseTemporaryRT(cmd);
        }

        private void SetScreenParams(CommandBuffer cmd)
        {
            var screenParams = new ScreenParams(Width, Height);

            if (!RenderToTexture)
            {
                screenParams.CameraRect = _camera.rect;
            }

            SetScreenParamsOverride(cmd, screenParams);
        }

        public void SetScreenParamsOverride(CommandBuffer cmd, in ScreenParams screenParams)
        {
            float width = screenParams.Width;
            float height = screenParams.Height;

            cmd.SetGlobalVector(ToonScreenParamsId, new Vector4(
                    1.0f / width,
                    1.0f / height,
                    width,
                    height
                )
            );
            cmd.SetGlobalVector(ScreenParamsId, new Vector4(
                    width,
                    height,
                    1.0f + 1.0f / width,
                    1.0f + 1.0f / height
                )
            );

            ref readonly Rect cameraRect = ref screenParams.CameraRect;

            if (screenParams.SetViewportRect)
            {
                var viewportRect = new Vector4(
                    1.0f / cameraRect.width, 1.0f / cameraRect.height,
                    -cameraRect.xMin / cameraRect.width, -cameraRect.yMin / cameraRect.height
                );
                cmd.SetGlobalVector("_ToonRP_ViewportRect", viewportRect);
            }

            float fullscreenWidth = width / cameraRect.width;
            float fullscreenHeight = height / cameraRect.height;
            cmd.SetGlobalVector("_ToonRP_FullScreenParams", new Vector4(
                    1.0f / fullscreenWidth,
                    1.0f / fullscreenHeight,
                    fullscreenWidth,
                    fullscreenHeight
                )
            );
        }

        private struct State
        {
            public CameraRtId ColorBufferId;
            public CameraRtId DepthBufferId;

            public CameraRtId MsColorBufferId;
            public CameraRtId MsDepthBufferId;
        }

        private struct CameraRtId
        {
            private readonly int _id;
            public readonly RenderTargetIdentifier Identifier;
            private readonly RenderTextureDescriptor _descriptor;
            private readonly FilterMode _filterMode;
            private RtState _state;

            private CameraRtId(int id, RenderTextureDescriptor descriptor, FilterMode filterMode)
            {
                _id = id;
                Identifier = _id;
                _descriptor = descriptor;
                _filterMode = filterMode;
                _state = RtState.TempNotAllocated;
            }

            private CameraRtId(RenderTargetIdentifier identifier)
            {
                _id = 0;
                Identifier = identifier;
                _descriptor = default;
                _filterMode = FilterMode.Point;
                _state = RtState.Persistent;
            }

            public static CameraRtId Temporary(int id, RenderTextureDescriptor descriptor, FilterMode filterMode) =>
                new(id, descriptor, filterMode);

            public static CameraRtId Persistent(RenderTargetIdentifier identifier) => new(identifier);

            private enum RtState
            {
                Invalid,
                Persistent,
                TempNotAllocated,
                TempAllocated,
            }

            public void EnsureTemporaryRT(CommandBuffer cmd)
            {
                Assert.IsTrue(IsValid);

                if (_state != RtState.TempNotAllocated)
                {
                    return;
                }

                Assert.IsTrue(_id != 0);

                cmd.GetTemporaryRT(_id, _descriptor, _filterMode);
                _state = RtState.TempAllocated;
            }

            public void ReleaseTemporaryRT(CommandBuffer cmd)
            {
                if (_state == RtState.TempAllocated)
                {
                    Assert.IsTrue(_id != 0);
                    cmd.ReleaseTemporaryRT(_id);
                    _state = RtState.TempNotAllocated;
                }
            }

            public bool IsValid => _state != RtState.Invalid;
            public bool IsAllocated => _state is RtState.Persistent or RtState.TempAllocated;
        }

        public struct ScreenParams
        {
            public readonly int Width;
            public readonly int Height;
            public Rect CameraRect;
            public bool SetViewportRect;

            public ScreenParams(int width, int height)
            {
                Width = width;
                Height = height;
                CameraRect = new Rect(0, 0, 1, 1);
                SetViewportRect = true;
            }
        }
    }
}