using DELTation.ToonRP.Extensions;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public class ToonDepthPrePass : ToonPrePassBase
    {
        private static readonly ShaderTagId DepthOnlyShaderTagId = new(ToonPasses.DepthOnly.LightMode);
        private static readonly ShaderTagId DepthNormalsShaderTagId = new(ToonPasses.DepthNormals.LightMode);

        private static readonly int DepthTextureId = Shader.PropertyToID("_ToonRP_DepthTexture");
        private static readonly int NormalsTextureId = Shader.PropertyToID("_ToonRP_NormalsTexture");

        private readonly int _depthTextureId;
        private readonly int _normalsTextureId;

        private Camera _camera;
        private ScriptableRenderContext _context;

        private CullingResults _cullingResults;
        private ToonRenderingExtensionsCollection _extensionsCollection;
        private bool _normals;
        private int _rtHeight;
        private int _rtWidth;
        private ToonCameraRendererSettings _settings;
        private bool _stencil;

        public ToonDepthPrePass() : this(DepthTextureId, NormalsTextureId) { }

        public ToonDepthPrePass(int depthTextureId, int normalsTextureId)
        {
            _depthTextureId = depthTextureId;
            _normalsTextureId = normalsTextureId;
        }

        public RenderTargetIdentifier DepthTexture { get; private set; }
        public RenderTargetIdentifier NormalsTexture { get; private set; }

        public void Setup(in ScriptableRenderContext context, in CullingResults cullingResults, Camera camera,
            ToonRenderingExtensionsCollection extensionsCollection,
            ToonAdditionalCameraData additionalCameraData,
            in ToonCameraRendererSettings settings, PrePassMode mode, int rtWidth, int rtHeight,
            bool stencil = false)
        {
            Assert.IsTrue(mode.Includes(PrePassMode.Depth), "mode.Includes(PrePassMode.Depth)");

            Setup(additionalCameraData);

            _context = context;
            _cullingResults = cullingResults;
            _camera = camera;
            _extensionsCollection = extensionsCollection;
            _settings = settings;
            _rtWidth = rtWidth;
            _rtHeight = rtHeight;
            _normals = mode.Includes(PrePassMode.Normals);
            _stencil = stencil;
        }

        public void Render()
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.DepthPrePass)))
            {
                var depthDesc = new RenderTextureDescriptor(_rtWidth, _rtHeight,
                    GraphicsFormat.None, ToonFormatUtils.GetDefaultDepthFormat(_stencil),
                    0
                );
                DepthTexture = GetTemporaryRT(cmd, _depthTextureId, depthDesc, FilterMode.Point);
                if (_normals)
                {
                    var normalsDesc = new RenderTextureDescriptor(_rtWidth, _rtHeight,
                        RenderTextureFormat.RGB565, 0
                    );
                    NormalsTexture = GetTemporaryRT(cmd, _normalsTextureId, normalsDesc, FilterMode.Point);
                    cmd.SetRenderTarget(
                        NormalsTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                        DepthTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                    );
                    cmd.ClearRenderTarget(true, true, Color.grey);
                }
                else
                {
                    cmd.SetRenderTarget(
                        DepthTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                    );
                    cmd.ClearRenderTarget(true, false, Color.clear);
                }


                _context.ExecuteCommandBufferAndClear(cmd);

                DrawRenderers(cmd);
            }

            _context.ExecuteCommandBufferAndClear(cmd);
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

            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void DrawRenderers(CommandBuffer cmd)
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
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, _camera.cullingMask);
            var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            _context.DrawRenderers(_cullingResults,
                ref drawingSettings, ref filteringSettings, ref renderStateBlock
            );

            _extensionsCollection.OnPrePass(
                _normals ? PrePassMode.Normals | PrePassMode.Depth : PrePassMode.Depth,
                ref _context, cmd,
                ref drawingSettings, ref filteringSettings, ref renderStateBlock
            );
        }
    }
}