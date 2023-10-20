using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public class MotionVectorsPrePass
    {
        private static readonly ShaderTagId MotionVectorsShaderTagId = new(ToonPasses.MotionVectors.LightMode);
        private static readonly int PrevViewProjMatrixId = Shader.PropertyToID("_PrevViewProjMatrix");
        private static readonly int NonJitteredViewProjMatrixId = Shader.PropertyToID("_NonJitteredViewProjMatrix");
        public readonly int MotionVectorsTextureId = Shader.PropertyToID("_ToonRP_MotionVectorsTexture");
        private ToonAdditionalCameraData _additionalCameraData;

        private Camera _camera;
        private ScriptableRenderContext _context;

        private CullingResults _cullingResults;
        private int _rtHeight;
        private int _rtWidth;
        private ToonCameraRendererSettings _settings;

        public void Setup(in ScriptableRenderContext context, in CullingResults cullingResults, Camera camera,
            ToonAdditionalCameraData additionalCameraData,
            in ToonCameraRendererSettings settings, int rtWidth, int rtHeight)
        {
            _context = context;
            _cullingResults = cullingResults;
            _camera = camera;
            _additionalCameraData = additionalCameraData;
            _settings = settings;
            _rtWidth = rtWidth;
            _rtHeight = rtHeight;
        }

        public void Render()
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            // This is required to compute previous object matrices
            _camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.MotionVectorsPrePass)))
            {
                cmd.SetGlobalMatrix(PrevViewProjMatrixId,
                    _additionalCameraData.MotionVectorsPersistentData.PreviousViewProjection
                );
                cmd.SetGlobalMatrix(NonJitteredViewProjMatrixId,
                    _additionalCameraData.MotionVectorsPersistentData.ViewProjection
                );

                cmd.GetTemporaryRT(MotionVectorsTextureId, _rtWidth, _rtHeight, 0, FilterMode.Point,
                    GraphicsFormat.R16G16_SFloat
                );
                cmd.SetRenderTarget(
                    MotionVectorsTextureId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    DepthPrePass.DepthTextureId, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
                );
                cmd.ClearRenderTarget(false, true, Color.black);

                _context.ExecuteCommandBufferAndClear(cmd);

                DrawRenderers();
            }

            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Cleanup()
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.ReleaseTemporaryRT(MotionVectorsTextureId);
            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void DrawRenderers()
        {
            var sortingSettings = new SortingSettings(_camera)
            {
                criteria = SortingCriteria.CommonOpaque,
            };
            var drawingSettings = new DrawingSettings(MotionVectorsShaderTagId, sortingSettings)
            {
                enableDynamicBatching = _settings.UseDynamicBatching,
                perObjectData = PerObjectData.MotionVectors,
            };
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, _camera.cullingMask)
            {
                excludeMotionVectorObjects = true,
            };

            var renderStateBlock = new RenderStateBlock(RenderStateMask.Depth)
            {
                depthState = new DepthState { writeEnabled = false, compareFunction = CompareFunction.LessEqual },
            };

            _context.DrawRenderers(_cullingResults,
                ref drawingSettings, ref filteringSettings, ref renderStateBlock
            );
        }
    }
}