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
        private bool _transientDepth;
        public bool ForceStoreAttachments { get; set; }

        public int MsaaSamples { get; private set; }

        public bool MemorylessMsaa =>
            ToonGraphicsDevice.SupportsMemorylessMsaa && UsingMsaa && !ForceStoreAttachments;

        public bool RenderToTexture { get; private set; }
        public GraphicsFormat DepthStencilFormat { get; private set; }

        public GraphicsFormat ColorFormat { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }

        public RenderTargetIdentifier ColorBufferId => RenderToTexture
            ? CameraColorBufferId
            : BuiltinRenderTextureType.CameraTarget;

        public RenderTargetIdentifier DepthBufferId => RenderToTexture
            ? CameraDepthBufferId
            : BuiltinRenderTextureType.Depth;

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


        public void InitializeAsSeparateRenderTexture(Camera camera, int width, int height,
            FilterMode filterMode,
            GraphicsFormat colorFormat, GraphicsFormat depthStencilFormat, int msaaSamples)
        {
            _transientDepth = false;
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
            _transientDepth = false;
            RenderToTexture = false;
            _camera = camera;
            Width = width;
            Height = height;
            ColorFormat = colorFormat;
            DepthStencilFormat = depthStencilFormat;
            MsaaSamples = msaaSamples;
        }

        public void GetTemporaryRTs(CommandBuffer cmd)
        {
            if (RenderToTexture)
            {
                _transientDepth = GetStoreActions().depthStoreAction == RenderBufferStoreAction.DontCare;

                var colorDesc = new RenderTextureDescriptor(Width, Height,
                    ColorFormat, 0, 1
                );
                var depthDesc = new RenderTextureDescriptor(Width, Height,
                    GraphicsFormat.None, DepthStencilFormat, 1
                );

                if (UsingMsaa && !MemorylessMsaa)
                {
                    colorDesc.msaaSamples = MsaaSamples;
                    depthDesc.msaaSamples = MsaaSamples;
                }

                cmd.GetTemporaryRT(CameraColorBufferId, colorDesc, _filterMode);

                if (!_transientDepth)
                {
                    cmd.GetTemporaryRT(CameraDepthBufferId, depthDesc, FilterMode.Point);
                }
            }
        }

        public void ReleaseTemporaryRTs(CommandBuffer cmd)
        {
            if (RenderToTexture)
            {
                cmd.ReleaseTemporaryRT(CameraColorBufferId);

                if (!_transientDepth)
                {
                    cmd.ReleaseTemporaryRT(CameraDepthBufferId);
                }
            }
        }

        public void BeginRenderPass(ref ScriptableRenderContext context, CommandBuffer cmd,
            RenderBufferLoadAction loadAction,
            in ToonClearValue clearValue = default)
        {
            // "Init" the render target. Prevents certain Unity bugs from happening...
            // if (loadAction != RenderBufferLoadAction.Load)
            // {
            //     if (_transientDepth)
            //     {
            //         cmd.SetRenderTarget(
            //             ColorBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare
            //         );    
            //     }
            //     else
            //     {
            //         cmd.SetRenderTarget(
            //             ColorBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare,
            //             RenderToTexture ? DepthBufferId : BuiltinRenderTextureType.CameraTarget,
            //             RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare
            //         );
            //     }
            //     
            //     context.ExecuteCommandBufferAndClear(cmd);
            // }

            var attachmentDescriptors =
                new NativeArray<AttachmentDescriptor>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            (RenderBufferStoreAction colorStoreAction, RenderBufferStoreAction depthStoreAction) = GetStoreActions();

            const int colorIndex = 0;
            const int depthIndex = 1;

            {
                var colorAttachment = new AttachmentDescriptor(ColorFormat)
                {
                    loadAction = loadAction,
                    storeAction = colorStoreAction,
                };

                if (clearValue.ClearColor)
                {
                    colorAttachment.ConfigureClear(clearValue.BackgroundColor);
                }

                if (colorStoreAction is RenderBufferStoreAction.Resolve or RenderBufferStoreAction.StoreAndResolve)
                {
                    colorAttachment.resolveTarget = ColorBufferId;
                }

                if (
                    loadAction == RenderBufferLoadAction.Load ||
                    colorStoreAction is RenderBufferStoreAction.Store or RenderBufferStoreAction.StoreAndResolve
                )
                {
                    colorAttachment.loadStoreTarget = ColorBufferId;
                }

                attachmentDescriptors[colorIndex] = colorAttachment;
            }

            {
                var depthAttachment = new AttachmentDescriptor(DepthStencilFormat)
                {
                    loadAction = loadAction,
                    storeAction = depthStoreAction,
                };

                if (clearValue.ClearDepth)
                {
                    depthAttachment.ConfigureClear(Color.black);
                }

                if (depthStoreAction is RenderBufferStoreAction.Resolve or RenderBufferStoreAction.StoreAndResolve)
                {
                    if (_transientDepth)
                    {
                        Debug.LogError("Cannot resolve to depth buffer: it is transient!");
                    }

                    depthAttachment.resolveTarget = DepthBufferId;
                }

                if (
                    loadAction == RenderBufferLoadAction.Load ||
                    depthStoreAction is RenderBufferStoreAction.Store or RenderBufferStoreAction.StoreAndResolve
                )
                {
                    if (_transientDepth)
                    {
                        Debug.LogError("Cannot load depth buffer: it is transient!");
                    }

                    depthAttachment.loadStoreTarget = DepthBufferId;
                }

                attachmentDescriptors[depthIndex] = depthAttachment;
            }

            context.ExecuteCommandBufferAndClear(cmd);
            context.BeginRenderPass(Width, Height, MsaaSamples, attachmentDescriptors, depthIndex);
            attachmentDescriptors.Dispose();

            var colorIndices = new NativeArray<int>(1, Allocator.Temp);
            colorIndices[0] = colorIndex;
            context.BeginSubPass(colorIndices);
            colorIndices.Dispose();

            cmd.SetViewport(RenderToTexture ? new Rect(0, 0, Width, Height) : _camera.pixelRect);
            context.ExecuteCommandBufferAndClear(cmd);
        }

        public void EndRenderPass(ref ScriptableRenderContext context)
        {
            context.EndSubPass();
            context.EndRenderPass();
        }

        private (RenderBufferStoreAction colorStoreAction, RenderBufferStoreAction depthStoreAction) GetStoreActions()
        {
            RenderBufferStoreAction storeAction =
                MemorylessMsaa ? RenderBufferStoreAction.Resolve : RenderBufferStoreAction.Store;
            RenderBufferStoreAction depthStoreAction =
                ForceStoreAttachments ? storeAction : RenderBufferStoreAction.DontCare;
            return (storeAction, depthStoreAction);
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