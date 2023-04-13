using UnityEngine;
using UnityEngine.Rendering;
using static DELTation.ToonRP.ToonCameraRendererSettings;

namespace DELTation.ToonRP
{
    public class DepthPrePass
    {
        private const string SampleName = "Depth Pre-Pass";
        private const int DepthBufferBits = 32;
        private static readonly int DepthTextureId = Shader.PropertyToID("_ToonRP_DepthTexture");
        private static readonly int NormalsTextureId = Shader.PropertyToID("_ToonRP_NormalsTexture");
        private static readonly ShaderTagId DepthOnlyShaderTagId = new("ToonRPDepthOnly");
        private static readonly ShaderTagId DepthNormalsShaderTagId = new("ToonRPDepthNormals");

        private readonly CommandBuffer _cmd = new()
        {
            name = SampleName,
        };
        private Camera _camera;
        private ScriptableRenderContext _context;

        private CullingResults _cullingResults;
        private bool _normals;
        private int _rtHeight;
        private int _rtWidth;
        private ToonCameraRendererSettings _settings;

        public void Setup(in ScriptableRenderContext context, in CullingResults cullingResults, Camera camera,
            in ToonCameraRendererSettings settings, int rtWidth, int rtHeight)
        {
            _context = context;
            _cullingResults = cullingResults;
            _camera = camera;
            _settings = settings;
            _rtWidth = rtWidth;
            _rtHeight = rtHeight;
            _normals = _settings.DepthPrePass == DepthPrePassMode.DepthNormals;
        }

        public void Render()
        {
            _cmd.GetTemporaryRT(DepthTextureId, _rtWidth, _rtHeight, DepthBufferBits, FilterMode.Point,
                RenderTextureFormat.Depth
            );
            if (_normals)
            {
                _cmd.GetTemporaryRT(NormalsTextureId, _rtWidth, _rtHeight, 0, FilterMode.Point,
                    RenderTextureFormat.RGB565
                );
                _cmd.SetRenderTarget(
                    NormalsTextureId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    DepthTextureId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                );
                _cmd.ClearRenderTarget(true, true, Color.grey);
            }
            else
            {
                _cmd.SetRenderTarget(
                    DepthTextureId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                );
                _cmd.ClearRenderTarget(true, false, Color.clear);
            }


            _cmd.BeginSample(SampleName);
            ExecuteBuffer();

            DrawRenderers();

            _cmd.EndSample(SampleName);
            ExecuteBuffer();
        }

        public void Cleanup()
        {
            _cmd.BeginSample(SampleName);
            _cmd.ReleaseTemporaryRT(DepthTextureId);
            if (_normals)
            {
                _cmd.ReleaseTemporaryRT(NormalsTextureId);
            }

            _cmd.EndSample(SampleName);
            ExecuteBuffer();
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

        private void ExecuteBuffer()
        {
            _context.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }
    }
}