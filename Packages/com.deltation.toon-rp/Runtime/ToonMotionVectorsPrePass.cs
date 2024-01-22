using DELTation.ToonRP.Extensions;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public class ToonMotionVectorsPrePass : ToonPrePassBase
    {
        private static readonly ShaderTagId MotionVectorsShaderTagId = new(ToonPasses.MotionVectors.LightMode);
        private static readonly int PrevViewProjMatrixId = Shader.PropertyToID("_PrevViewProjMatrix");
        private static readonly int NonJitteredViewProjMatrixId = Shader.PropertyToID("_NonJitteredViewProjMatrix");
        private static readonly int MotionVectorsTextureId = Shader.PropertyToID("_ToonRP_MotionVectorsTexture");
        private readonly ToonDepthPrePass _depthPrePass;

        private Camera _camera;
        private ScriptableRenderContext _context;

        private CullingResults _cullingResults;
        private ToonRenderingExtensionsCollection _extensionsCollection;
        private ToonMotionVectorsPersistentData _motionVectorsPersistentData;
        private int _rtHeight;
        private int _rtWidth;
        private ToonCameraRendererSettings _settings;

        public ToonMotionVectorsPrePass(ToonDepthPrePass depthPrePass) => _depthPrePass = depthPrePass;

        public RenderTargetIdentifier MotionVectorsTexture { get; private set; }

        public void Setup(in ScriptableRenderContext context, in CullingResults cullingResults, Camera camera,
            ToonRenderingExtensionsCollection extensionsCollection,
            ToonAdditionalCameraData additionalCameraData,
            in ToonCameraRendererSettings settings, int rtWidth, int rtHeight)
        {
            Setup(additionalCameraData);

            _context = context;
            _cullingResults = cullingResults;
            _camera = camera;
            _extensionsCollection = extensionsCollection;
            _motionVectorsPersistentData = additionalCameraData.GetPersistentData<ToonMotionVectorsPersistentData>();
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
                cmd.SetGlobalMatrix(PrevViewProjMatrixId, _motionVectorsPersistentData.PreviousViewProjection);
                cmd.SetGlobalMatrix(NonJitteredViewProjMatrixId, _motionVectorsPersistentData.ViewProjection);

                var desc = new RenderTextureDescriptor(_rtWidth, _rtHeight,
                    GraphicsFormat.R16G16_SFloat, 0
                );
                MotionVectorsTexture = GetTemporaryRT(cmd, MotionVectorsTextureId, desc, FilterMode.Point);
                cmd.SetRenderTarget(
                    MotionVectorsTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    _depthPrePass.DepthTexture, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
                );
                cmd.ClearRenderTarget(false, true, Color.black);

                _context.ExecuteCommandBufferAndClear(cmd);

                DrawRenderers(cmd);
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

        private void DrawRenderers(CommandBuffer cmd)
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
                depthState = new DepthState { writeEnabled = true, compareFunction = CompareFunction.LessEqual },
            };

            _context.DrawRenderers(_cullingResults,
                ref drawingSettings, ref filteringSettings, ref renderStateBlock
            );

            _extensionsCollection.OnPrePass(PrePassMode.MotionVectors,
                ref _context, cmd,
                ref drawingSettings, ref filteringSettings, ref renderStateBlock
            );
        }
    }
}