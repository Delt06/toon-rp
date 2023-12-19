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
        public static readonly int CameraColorBufferId = Shader.PropertyToID("_ToonRP_CameraColorBuffer");
        public static readonly int CameraDepthBufferId = Shader.PropertyToID("_ToonRP_CameraDepthBuffer");

        private Camera _camera;
        private FilterMode _filterMode;
        private bool _inRenderPass;
        private RenderBufferLoadAction _lastRenderPassLoadAction;

        public bool ForceStoreAttachments { get; set; } = true;

        public bool ForceDisableNativeRenderPass { get; set; }
        public int MsaaSamples { get; private set; }
        public bool RenderToTexture { get; private set; }
        public GraphicsFormat DepthStencilFormat { get; private set; }


        public bool UseNativeRenderPass =>
            !ForceStoreAttachments && !ForceDisableNativeRenderPass &&
            SystemInfo.graphicsDeviceType is GraphicsDeviceType.Vulkan or GraphicsDeviceType.Metal;

        public GraphicsFormat ColorFormat { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }


        public RenderTargetIdentifier ColorBufferId => RenderToTexture
            ? CameraColorBufferId
            : _camera.targetTexture != null
                ? _camera.targetTexture.colorBuffer
                : BuiltinRenderTextureType.CameraTarget;

        public RenderTargetIdentifier DepthBufferId => RenderToTexture
            ? CameraDepthBufferId
            : _camera.targetTexture != null
                ? _camera.targetTexture.depthBuffer
                : BuiltinRenderTextureType.CameraTarget;

        public bool UsingMsaa => MsaaSamples > 1;

        public void FinalBlit(CommandBuffer cmd)
        {
            if (RenderToTexture)
            {
                cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

                var screenParams = new ScreenParams(_camera.pixelWidth, _camera.pixelHeight)
                {
                    CameraRect = _camera.rect,
                    SetViewportRect = false,
                };
                SetScreenParamsOverride(cmd, screenParams);
                cmd.SetViewport(_camera.pixelRect);

                ToonBlitter.BlitDefault(cmd, CameraColorBufferId);
            }
        }


        public void InitializeAsSeparateRenderTexture(CommandBuffer cmd, Camera camera, int width, int height,
            FilterMode filterMode,
            GraphicsFormat colorFormat, GraphicsFormat depthStencilFormat, int msaaSamples)
        {
            _filterMode = filterMode;
            RenderToTexture = true;
            _camera = camera;
            Width = width;
            Height = height;
            ColorFormat = colorFormat;
            MsaaSamples = msaaSamples;
            DepthStencilFormat = depthStencilFormat;
        }

        public void InitializeAsCameraRenderTarget(Camera camera, int width, int height,
            GraphicsFormat colorFormat, GraphicsFormat depthStencilFormat, int msaaSamples)
        {
            RenderToTexture = false;
            _camera = camera;
            Width = width;
            Height = height;
            ColorFormat = colorFormat;
            MsaaSamples = msaaSamples;
            DepthStencilFormat = depthStencilFormat;
        }

        public void GetTemporaryRTs(CommandBuffer cmd)
        {
            if (RenderToTexture)
            {
                var colorDesc = new RenderTextureDescriptor(Width, Height,
                    ColorFormat, 0, 1
                );

                if (!UseNativeRenderPass)
                {
                    var depthDesc = new RenderTextureDescriptor(Width, Height,
                        GraphicsFormat.None, DepthStencilFormat,
                        1
                    );

                    colorDesc.msaaSamples = MsaaSamples;
                    depthDesc.msaaSamples = MsaaSamples;
                    depthDesc.memoryless = ForceStoreAttachments
                        ? RenderTextureMemoryless.None
                        : RenderTextureMemoryless.Depth;
                    cmd.GetTemporaryRT(CameraDepthBufferId, depthDesc, FilterMode.Point);
                }

                cmd.GetTemporaryRT(CameraColorBufferId, colorDesc, _filterMode);
            }
        }

        public void BeginRenderPass(ref ScriptableRenderContext context, RenderBufferLoadAction loadAction,
            in ToonClearValue clearValue = default)
        {
            Assert.IsFalse(_inRenderPass);

            CommandBuffer cmd = CommandBufferPool.Get();

            _lastRenderPassLoadAction = loadAction;
            if (UseNativeRenderPass && IsLoadActionSupportedByNativeRenderPass(loadAction))
            {
                BeginNativeRenderPass(ref context, cmd, loadAction, clearValue);
            }
            else
            {
                BeginRenderPassFallback(loadAction, clearValue, cmd);
            }

            SetScreenParams(cmd);
            context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
            _inRenderPass = true;
        }

        private void BeginNativeRenderPass(ref ScriptableRenderContext context, CommandBuffer cmd,
            RenderBufferLoadAction loadAction,
            ToonClearValue clearValue)
        {
            // "Init" the render target. Prevents certain Unity bugs from happening (e.g., misplaced immediate GUI). 
            if (loadAction != RenderBufferLoadAction.Load)
            {
                cmd.SetRenderTarget(
                    ColorBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare
                );
                context.ExecuteCommandBufferAndClear(cmd);
            }

            const int colorIndex = 0;
            const int depthIndex = 1;

            if (!IsLoadActionSupportedByNativeRenderPass(loadAction))
            {
                Debug.LogError("Load action is not supported for native render passes.");
            }

            {
                var attachments =
                    new NativeArray<AttachmentDescriptor>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory
                    );

                var colorAttachment = new AttachmentDescriptor(ColorFormat);
                {
                    colorAttachment.loadAction = loadAction;
                    colorAttachment.storeAction =
                        UsingMsaa ? RenderBufferStoreAction.Resolve : RenderBufferStoreAction.Store;
                    if (clearValue.ClearColor)
                    {
                        colorAttachment.ConfigureClear(clearValue.BackgroundColor);
                    }

                    if (colorAttachment.loadAction == RenderBufferLoadAction.Load ||
                        colorAttachment.storeAction == RenderBufferStoreAction.Store)
                    {
                        colorAttachment.loadStoreTarget = ColorBufferId;
                    }

                    if (colorAttachment.storeAction == RenderBufferStoreAction.Resolve)
                    {
                        colorAttachment.resolveTarget = ColorBufferId;
                    }
                }

                var depthAttachment = new AttachmentDescriptor(DepthStencilFormat);
                {
                    depthAttachment.loadAction = loadAction;
                    depthAttachment.storeAction = RenderBufferStoreAction.DontCare;
                    if (clearValue.ClearDepth)
                    {
                        depthAttachment.ConfigureClear(Color.black);
                    }

                    if (depthAttachment.loadAction == RenderBufferLoadAction.Load ||
                        depthAttachment.storeAction == RenderBufferStoreAction.Store ||
                        DepthBufferId == BuiltinRenderTextureType.CameraTarget
                        )
                    {
                        depthAttachment.loadStoreTarget = BuiltinRenderTextureType.Depth;
                    }
                }

                attachments[colorIndex] = colorAttachment;
                attachments[depthIndex] = depthAttachment;

                Rect cameraRect = _camera.rect;
                int fullScreenWidth = (int) (Width / cameraRect.width);
                int fullScreenHeight = (int) (Height / cameraRect.height);
                context.BeginRenderPass(fullScreenWidth, fullScreenHeight, MsaaSamples, attachments, depthIndex);
                attachments.Dispose();
            }

            {
                var colorIndices = new NativeArray<int>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                colorIndices[0] = colorIndex;
                context.BeginSubPass(colorIndices);
                colorIndices.Dispose();
            }

            SetViewport(cmd);
        }

        private void SetViewport(CommandBuffer cmd)
        {
            cmd.SetViewport(RenderToTexture ? new Rect(0, 0, Width, Height) : _camera.pixelRect);
        }

        private void BeginRenderPassFallback(RenderBufferLoadAction loadAction, in ToonClearValue clearValue,
            CommandBuffer cmd)
        {
            const RenderBufferStoreAction colorStoreAction = RenderBufferStoreAction.Store;
            RenderBufferStoreAction depthStoreAction =
                ForceStoreAttachments ? RenderBufferStoreAction.Store : RenderBufferStoreAction.DontCare;

            if (RenderToTexture)
            {
                cmd.SetRenderTarget(
                    CameraColorBufferId, loadAction, colorStoreAction,
                    CameraDepthBufferId, loadAction, depthStoreAction
                );
            }
            else
            {
                cmd.SetRenderTarget(
                    BuiltinRenderTextureType.CameraTarget, loadAction, colorStoreAction,
                    BuiltinRenderTextureType.CameraTarget, loadAction, depthStoreAction
                );
            }

            SetViewport(cmd);
            cmd.ClearRenderTarget(clearValue.ClearDepth, clearValue.ClearColor, clearValue.BackgroundColor);
        }

        public void EndRenderPass(ref ScriptableRenderContext context)
        {
            Assert.IsTrue(_inRenderPass);

            if (UseNativeRenderPass && IsLoadActionSupportedByNativeRenderPass(_lastRenderPassLoadAction))
            {
                context.EndSubPass();
                context.EndRenderPass();
            }

            _lastRenderPassLoadAction = RenderBufferLoadAction.DontCare;
            _inRenderPass = false;
        }

        private bool IsLoadActionSupportedByNativeRenderPass(RenderBufferLoadAction loadAction) =>
            loadAction != RenderBufferLoadAction.Load;

        public void ReleaseTemporaryRTs(CommandBuffer cmd)
        {
            if (RenderToTexture)
            {
                cmd.ReleaseTemporaryRT(CameraColorBufferId);
                if (!UseNativeRenderPass)
                {
                    cmd.ReleaseTemporaryRT(CameraDepthBufferId);
                }
            }
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

        public struct ScreenParams
        {
            public int Width;
            public int Height;
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