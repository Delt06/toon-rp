using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public sealed class ToonCameraRenderTarget
    {
        public static readonly int CameraColorBufferId = Shader.PropertyToID("_ToonRP_CameraColorBuffer");
        public static readonly int CameraDepthBufferId = Shader.PropertyToID("_ToonRP_CameraDepthBuffer");

        public int MsaaSamples { get; private set; }
        public bool RenderToTexture { get; private set; }
        public GraphicsFormat DepthStencilFormat { get; private set; }

        public RenderTextureFormat ColorFormat { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }

        public void FinalBlit(CommandBuffer cmd)
        {
            if (RenderToTexture)
            {
                cmd.Blit(CameraColorBufferId, BuiltinRenderTextureType.CameraTarget);
            }
        }


        public void InitializeAsSeparateRenderTexture(CommandBuffer cmd, int width, int height, FilterMode filterMode,
            RenderTextureFormat colorFormat, GraphicsFormat depthStencilFormat, int msaaSamples)
        {
            RenderToTexture = true;
            Width = width;
            Height = height;
            ColorFormat = colorFormat;
            MsaaSamples = msaaSamples;
            DepthStencilFormat = depthStencilFormat;

            cmd.GetTemporaryRT(
                CameraColorBufferId, width, height, 0,
                filterMode, colorFormat,
                RenderTextureReadWrite.Default, msaaSamples
            );

            var depthDesc = new RenderTextureDescriptor(width, height,
                GraphicsFormat.None, depthStencilFormat,
                0
            )
            {
                msaaSamples = msaaSamples,
            };
            cmd.GetTemporaryRT(CameraDepthBufferId, depthDesc, FilterMode.Point);
        }

        public void SetRenderTarget(CommandBuffer cmd)
        {
            if (RenderToTexture)
            {
                cmd.SetRenderTarget(
                    CameraColorBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    CameraDepthBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                );
            }
            else
            {
                cmd.SetRenderTarget(
                    BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store
                );
            }
        }

        public void InitializeAsCameraRenderTarget(int width, int height, RenderTextureFormat colorFormat)
        {
            RenderToTexture = false;
            Width = width;
            Height = height;
            ColorFormat = colorFormat;
        }

        public void ReleaseTemporaryRTs(CommandBuffer cmd)
        {
            if (RenderToTexture)
            {
                cmd.ReleaseTemporaryRT(CameraColorBufferId);
                cmd.ReleaseTemporaryRT(CameraDepthBufferId);
            }
        }
    }
}