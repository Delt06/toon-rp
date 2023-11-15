using Unity.Collections;
using UnityEngine;
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
            : BuiltinRenderTextureType.CameraTarget;

        public bool UsingMsaa => MsaaSamples > 1;

        public void FinalBlit(CommandBuffer cmd)
        {
            if (RenderToTexture)
            {
                cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
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
            GraphicsFormat colorFormat, GraphicsFormat depthStencilFormat)
        {
            RenderToTexture = false;
            _camera = camera;
            Width = width;
            Height = height;
            ColorFormat = colorFormat;
            MsaaSamples = 1;
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
            CommandBuffer cmd = CommandBufferPool.Get();

            if (UseNativeRenderPass)
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

            if (loadAction == RenderBufferLoadAction.Load)
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

                    if (!RenderToTexture || colorAttachment.storeAction == RenderBufferStoreAction.Store)
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

                    if (!RenderToTexture)
                    {
                        depthAttachment.loadStoreTarget = BuiltinRenderTextureType.Depth;
                    }
                }

                attachments[colorIndex] = colorAttachment;
                attachments[depthIndex] = depthAttachment;

                context.BeginRenderPass(Width, Height, MsaaSamples, attachments, depthIndex);
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
            if (UseNativeRenderPass)
            {
                context.EndSubPass();
                context.EndRenderPass();
            }
        }

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

        public void SetScreenParams(CommandBuffer cmd)
        {
            cmd.SetGlobalVector(ToonScreenParamsId, new Vector4(
                    1.0f / Width,
                    1.0f / Height,
                    Width,
                    Height
                )
            );
            cmd.SetGlobalVector(ScreenParamsId, new Vector4(
                    Width,
                    Height,
                    1.0f + 1.0f / Width,
                    1.0f + 1.0f / Height
                )
            );
        }

        public void SetScreenParamsOverride(CommandBuffer cmd, int width, int height)
        {
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
        }
    }
}