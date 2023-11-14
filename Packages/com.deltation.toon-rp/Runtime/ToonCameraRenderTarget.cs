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
        public bool StoreDepthAttachment { get; set; } = true;

        public int MsaaSamples { get; private set; }
        public bool RenderToTexture { get; private set; }
        public GraphicsFormat DepthStencilFormat { get; private set; }

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
            RenderToTexture = true;
            _camera = camera;
            Width = width;
            Height = height;
            ColorFormat = colorFormat;
            MsaaSamples = msaaSamples;
            DepthStencilFormat = depthStencilFormat;

            var colorDesc = new RenderTextureDescriptor(width, height,
                colorFormat, 0, 1
            )
            {
                msaaSamples = msaaSamples,
            };

            cmd.GetTemporaryRT(
                CameraColorBufferId, colorDesc, filterMode
            );

            var depthDesc = new RenderTextureDescriptor(width, height,
                GraphicsFormat.None, depthStencilFormat,
                1
            )
            {
                msaaSamples = msaaSamples,
                memoryless = StoreDepthAttachment ? RenderTextureMemoryless.None : RenderTextureMemoryless.Depth,
            };
            if (!StoreDepthAttachment)
            {
                depthDesc.memoryless |= RenderTextureMemoryless.Depth;
            }
            
            cmd.GetTemporaryRT(CameraDepthBufferId, depthDesc, FilterMode.Point);
        }

        public void SetRenderTarget(CommandBuffer cmd, RenderBufferLoadAction loadAction)
        {
            if (RenderToTexture)
            {
                RenderBufferStoreAction storeAction =
                    UsingMsaa ? RenderBufferStoreAction.Resolve : RenderBufferStoreAction.Store;
                RenderBufferStoreAction depthStoreAction =
                    StoreDepthAttachment ? storeAction : RenderBufferStoreAction.DontCare;
                cmd.SetRenderTarget(
                    CameraColorBufferId, loadAction, storeAction,
                    CameraDepthBufferId, loadAction, depthStoreAction
                );
            }
            else
            {
                const RenderBufferStoreAction colorStoreAction = RenderBufferStoreAction.Store;
                RenderBufferStoreAction depthStoreAction =
                    StoreDepthAttachment ? RenderBufferStoreAction.Store : RenderBufferStoreAction.DontCare;
                cmd.SetRenderTarget(
                    BuiltinRenderTextureType.CameraTarget, loadAction, colorStoreAction,
                    BuiltinRenderTextureType.CameraTarget, loadAction, depthStoreAction
                );
                cmd.SetViewport(_camera.pixelRect);
            }

            SetScreenParams(cmd);
        }

        public void InitializeAsCameraRenderTarget(Camera camera, int width, int height,
            GraphicsFormat colorFormat)
        {
            RenderToTexture = false;
            _camera = camera;
            Width = width;
            Height = height;
            ColorFormat = colorFormat;
            MsaaSamples = 1;
        }

        public void ReleaseTemporaryRTs(CommandBuffer cmd)
        {
            if (RenderToTexture)
            {
                cmd.ReleaseTemporaryRT(CameraColorBufferId);
                cmd.ReleaseTemporaryRT(CameraDepthBufferId);
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