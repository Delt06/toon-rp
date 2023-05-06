using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using static DELTation.ToonRP.ToonCameraRendererSettings;

namespace DELTation.ToonRP
{
    public class DepthPrePass
    {
        private const int DepthBufferBits = 32;
        private static readonly int DepthTextureId = Shader.PropertyToID("_ToonRP_DepthTexture");
        private static readonly int NormalsTextureId = Shader.PropertyToID("_ToonRP_NormalsTexture");
        private static readonly ShaderTagId DepthOnlyShaderTagId = new("ToonRPDepthOnly");
        private static readonly ShaderTagId DepthNormalsShaderTagId = new("ToonRPDepthNormals");

        private Camera _camera;
        private ScriptableRenderContext _context;

        private CullingResults _cullingResults;
        private bool _normals;
        private int _rtHeight;
        private int _rtWidth;
        private ToonCameraRendererSettings _settings;

        public void Setup(in ScriptableRenderContext context, in CullingResults cullingResults, Camera camera,
            in ToonCameraRendererSettings settings, DepthPrePassMode mode, int rtWidth, int rtHeight)
        {
            Assert.IsTrue(mode != DepthPrePassMode.Off, "mode != DepthPrePassMode.Off");

            _context = context;
            _cullingResults = cullingResults;
            _camera = camera;
            _settings = settings;
            _rtWidth = rtWidth;
            _rtHeight = rtHeight;
            _normals = mode == DepthPrePassMode.DepthNormals;
        }

        public void Render()
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.DepthPrePass)))
            {
                cmd.GetTemporaryRT(DepthTextureId, _rtWidth, _rtHeight, DepthBufferBits, FilterMode.Point,
                    RenderTextureFormat.Depth
                );
                if (_normals)
                {
                    cmd.GetTemporaryRT(NormalsTextureId, _rtWidth, _rtHeight, 0, FilterMode.Point,
                        RenderTextureFormat.RGB565
                    );
                    cmd.SetRenderTarget(
                        NormalsTextureId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                        DepthTextureId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                    );
                    cmd.ClearRenderTarget(true, true, Color.grey);
                }
                else
                {
                    cmd.SetRenderTarget(
                        DepthTextureId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
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
            cmd.ReleaseTemporaryRT(DepthTextureId);
            if (_normals)
            {
                cmd.ReleaseTemporaryRT(NormalsTextureId);
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