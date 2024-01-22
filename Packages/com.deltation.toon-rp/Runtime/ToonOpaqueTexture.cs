using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public class ToonOpaqueTexture
    {
        private static readonly int OpaqueTextureId = Shader.PropertyToID("_ToonRP_OpaqueTexture");

        private readonly ToonCameraRenderTarget _renderTarget;
        private ScriptableRenderContext _context;

        public ToonOpaqueTexture(ToonCameraRenderTarget renderTarget) => _renderTarget = renderTarget;
        public bool Enabled { get; private set; }

        public void Setup(ref ScriptableRenderContext context, in ToonCameraRendererSettings settings)
        {
            _context = context;
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

                cmd.GetTemporaryRT(OpaqueTextureId, desc);
                cmd.SetRenderTarget(OpaqueTextureId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                ToonBlitter.BlitDefault(cmd, _renderTarget.CurrentColorBufferId(), true);
                _context.ExecuteCommandBufferAndClear(cmd);

                _renderTarget.BeginRenderPass(ref _context, RenderBufferLoadAction.Load);
            }

            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
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