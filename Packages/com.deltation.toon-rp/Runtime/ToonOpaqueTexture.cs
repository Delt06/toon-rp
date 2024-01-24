using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public class ToonOpaqueTexture
    {
        private static readonly int OpaqueTextureId = Shader.PropertyToID("_ToonRP_OpaqueTexture");

        private readonly ToonCameraRenderTarget _renderTarget;
        private ToonAdditionalCameraData _additionalCameraData;
        private ScriptableRenderContext _context;

        public ToonOpaqueTexture(ToonCameraRenderTarget renderTarget) => _renderTarget = renderTarget;
        public bool Enabled { get; private set; }

        public void Setup(ref ScriptableRenderContext context, ToonAdditionalCameraData additionalCameraData,
            in ToonCameraRendererSettings settings)
        {
            _context = context;
            _additionalCameraData = additionalCameraData;
            Enabled = settings.OpaqueTexture;
        }

        public void Capture()
        {
            if (!Enabled)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.OpaqueTexture)))
            {
                _context.ExecuteCommandBufferAndClear(cmd);

                _renderTarget.EndRenderPass(ref _context, cmd);

                int rtWidth = _renderTarget.Width;
                int rtHeight = _renderTarget.Height;
                var desc = new RenderTextureDescriptor(rtWidth, rtHeight,
                    _renderTarget.ColorFormat,
                    0, 1
                );

                RenderTargetIdentifier opaqueTexture = GetTemporaryRT(cmd, OpaqueTextureId, desc, FilterMode.Bilinear);
                cmd.SetRenderTarget(opaqueTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                ToonBlitter.BlitDefault(cmd, _renderTarget.CurrentColorBufferId(), true);
                _context.ExecuteCommandBufferAndClear(cmd);

                _renderTarget.BeginRenderPass(ref _context, RenderBufferLoadAction.Load);
            }

            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        private RenderTargetIdentifier GetTemporaryRT(CommandBuffer cmd,
            int identifier, RenderTextureDescriptor descriptor, FilterMode filterMode)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            XRPass xrPass = _additionalCameraData.XrPass;
            if (xrPass.enabled)
            {
                int arraySize = xrPass.viewCount;
                cmd.GetTemporaryRTArray(identifier, descriptor.width, descriptor.height, arraySize,
                    descriptor.depthBufferBits, filterMode, descriptor.graphicsFormat
                );
                return ToonRpUtils.FixupTextureArrayIdentifier(identifier);
            }
#endif // ENABLE_VR && ENABLE_XR_MODULE

            cmd.GetTemporaryRT(identifier, descriptor, filterMode);
            return identifier;
        }

        public void Cleanup()
        {
            if (!Enabled)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.ReleaseTemporaryRT(OpaqueTextureId);
            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}