using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public class DepthPrePass
    {
        private static readonly ShaderTagId DepthOnlyShaderTagId = new("ToonRPDepthOnly");
        private static readonly ShaderTagId DepthNormalsShaderTagId = new("ToonRPDepthNormals");
        private readonly int _depthTextureId;
        private readonly int _normalsTextureId;

        private Camera _camera;
        private ScriptableRenderContext _context;

        private CullingResults _cullingResults;
        private bool _normals;
        private int _rtHeight;
        private int _rtWidth;
        private ToonCameraRendererSettings _settings;
        private bool _stencil;

        public DepthPrePass() : this(
            Shader.PropertyToID("_ToonRP_DepthTexture"), Shader.PropertyToID("_ToonRP_NormalsTexture")
        ) { }

        public DepthPrePass(int depthTextureId, int normalsTextureId)
        {
            _depthTextureId = depthTextureId;
            _normalsTextureId = normalsTextureId;
        }

        public void Setup(in ScriptableRenderContext context, in CullingResults cullingResults, Camera camera,
            in ToonCameraRendererSettings settings, DepthPrePassMode mode, int rtWidth, int rtHeight,
            bool stencil = false)
        {
            Assert.IsTrue(mode != DepthPrePassMode.Off, "mode != DepthPrePassMode.Off");

            _context = context;
            _cullingResults = cullingResults;
            _camera = camera;
            _settings = settings;
            _rtWidth = rtWidth;
            _rtHeight = rtHeight;
            _normals = mode == DepthPrePassMode.DepthNormals;
            _stencil = stencil;
        }

        public void Render()
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.DepthPrePass)))
            {
                var depthDesc = new RenderTextureDescriptor(_rtWidth, _rtHeight,
                    GraphicsFormat.None, _stencil ? GraphicsFormat.D24_UNorm_S8_UInt : GraphicsFormat.D32_SFloat,
                    0
                );
                cmd.GetTemporaryRT(_depthTextureId, depthDesc);
                if (_normals)
                {
                    cmd.GetTemporaryRT(_normalsTextureId, _rtWidth, _rtHeight, 0, FilterMode.Point,
                        RenderTextureFormat.RGB565
                    );
                    cmd.SetRenderTarget(
                        _normalsTextureId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                        _depthTextureId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                    );
                    cmd.ClearRenderTarget(true, true, Color.grey);
                }
                else
                {
                    cmd.SetRenderTarget(
                        _depthTextureId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                    );
                    cmd.ClearRenderTarget(true, false, Color.clear);
                }


                ExecuteBuffer(cmd);

                DrawRenderers();
            }

            ExecuteBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Cleanup()
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.ReleaseTemporaryRT(_depthTextureId);
            if (_normals)
            {
                cmd.ReleaseTemporaryRT(_normalsTextureId);
            }

            ExecuteBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void DrawRenderers()
        {
            var sortingSettings = new SortingSettings(_camera)
            {
                criteria = SortingCriteria.CommonOpaque,
            };
            ShaderTagId shaderPassName = _normals ? DepthNormalsShaderTagId : DepthOnlyShaderTagId;
            var drawingSettings = new DrawingSettings(shaderPassName, sortingSettings)
            {
                enableDynamicBatching = _settings.UseDynamicBatching,
            };
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

            _context.DrawRenderers(_cullingResults,
                ref drawingSettings, ref filteringSettings
            );
        }

        private void ExecuteBuffer(CommandBuffer cmd)
        {
            _context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}