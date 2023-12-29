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

        private Camera _camera;
        private FilterMode _filterMode;
        private bool _inRenderPass;

        private State _state;

        public bool ForceStoreAttachments { get; set; } = true;

        public int MsaaSamples { get; private set; }
        public bool RenderToTexture { get; private set; }
        public GraphicsFormat DepthStencilFormat { get; private set; }

        public GraphicsFormat ColorFormat { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }


        public RenderTargetIdentifier ColorBufferId => RenderToTexture
            ? RenderToTextureColorBufferId
            : _camera.targetTexture != null
                ? _camera.targetTexture.colorBuffer
                : BuiltinRenderTextureType.CameraTarget;

        public RenderTargetIdentifier DepthBufferId => RenderToTexture
            ? RenderToTextureDepthBufferId
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

                ToonBlitter.BlitDefault(cmd, RenderToTextureColorBufferId);
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
            _state = default;
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
            _state = default;
        }

        public void GetTemporaryRTs(CommandBuffer cmd)
        {
            if (RenderToTexture)
            {
                var colorDesc = new RenderTextureDescriptor(Width, Height,
                    ColorFormat, 0, 1
                );
                cmd.GetTemporaryRT(RenderToTextureColorBufferId, colorDesc, _filterMode);
                _state.ColorBufferId = RenderToTextureColorBufferId;
                _state.ColorBufferTempId = RenderToTextureColorBufferId;

                if (ForceStoreAttachments)
                {
                    var depthDesc = new RenderTextureDescriptor(Width, Height,
                        GraphicsFormat.None, DepthStencilFormat, 1
                    );
                    cmd.GetTemporaryRT(RenderToTextureDepthBufferId, depthDesc, FilterMode.Point);
                    _state.DepthBufferId = RenderToTextureDepthBufferId;
                    _state.DepthBufferTempId = RenderToTextureDepthBufferId;
                }
            }
            else
            {
                RenderTexture targetTexture = _camera.targetTexture;
                (RenderTargetIdentifier colorBufferId, RenderTargetIdentifier depthBufferId) = targetTexture != null
                    ? ((RenderTargetIdentifier) targetTexture.colorBuffer,
                        (RenderTargetIdentifier) targetTexture.depthBuffer)
                    : (BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget);

                _state.ColorBufferId = colorBufferId;
                if (ForceStoreAttachments)
                {
                    _state.DepthBufferId = depthBufferId;
                }
            }
        }

        public void BeginRenderPass(ref ScriptableRenderContext context, RenderBufferLoadAction loadAction,
            in ToonClearValue clearValue = default)
        {
            Assert.IsFalse(_inRenderPass);

            CommandBuffer cmd = CommandBufferPool.Get();

            BeginNativeRenderPass(ref context, cmd, loadAction, clearValue);

            SetScreenParams(cmd);
            context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
            _inRenderPass = true;
        }

        private void BeginNativeRenderPass(ref ScriptableRenderContext context, CommandBuffer cmd,
            RenderBufferLoadAction loadAction,
            ToonClearValue clearValue)
        {
            // // "Init" the render target. Prevents certain Unity bugs from happening (e.g., misplaced immediate GUI). 
            // if (loadAction != RenderBufferLoadAction.Load)
            // {
            //     cmd.SetRenderTarget(
            //         ColorBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare
            //     );
            //     context.ExecuteCommandBufferAndClear(cmd);
            // }

            const int colorIndex = 0;
            const int depthIndex = 1;

            {
                var attachments =
                    new NativeArray<AttachmentDescriptor>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory
                    );

                var colorAttachment = new AttachmentDescriptor(ColorFormat);
                RenderBufferStoreAction storeAction =
                    UsingMsaa ? RenderBufferStoreAction.Resolve : RenderBufferStoreAction.Store;
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
                        colorAttachment.loadStoreTarget = _state.ColorBufferId;
                    }

                    if (colorAttachment.storeAction == RenderBufferStoreAction.Resolve)
                    {
                        colorAttachment.resolveTarget = _state.ColorBufferId;
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
                        depthAttachment.storeAction == RenderBufferStoreAction.Store
                       )
                    {
                        depthAttachment.loadStoreTarget = _state.DepthBufferId;
                    }

                    if (depthAttachment.storeAction == RenderBufferStoreAction.Resolve)
                    {
                        depthAttachment.resolveTarget = _state.ColorBufferId;
                    }

                    // specifying camera depth more precisely is required here
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

                Rect cameraRect = _camera.rect;
                int fullScreenWidth = Width, fullScreenHeight = Height;
                if (_camera.rect != new Rect(0, 0, 1, 1))
                {
                    fullScreenWidth = (int) (fullScreenWidth / cameraRect.width);
                    fullScreenHeight = (int) (fullScreenHeight / cameraRect.height);
                }

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

        public void EndRenderPass(ref ScriptableRenderContext context)
        {
            Assert.IsTrue(_inRenderPass);

            context.EndSubPass();
            context.EndRenderPass();

            _inRenderPass = false;
        }

        public void ReleaseTemporaryRTs(CommandBuffer cmd)
        {
            if (_state.ColorBufferTempId != 0)
            {
                cmd.ReleaseTemporaryRT(_state.ColorBufferTempId);
            }

            if (_state.DepthBufferTempId != 0)
            {
                cmd.ReleaseTemporaryRT(_state.DepthBufferTempId);
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

        public RenderTargetIdentifier CurrentColorBufferId() => _state.ColorBufferId;

        private struct State
        {
            public RenderTargetIdentifier ColorBufferId;
            public int ColorBufferTempId;
            public RenderTargetIdentifier DepthBufferId;
            public int DepthBufferTempId;
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