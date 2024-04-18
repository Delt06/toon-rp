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
        private static readonly int ZeroMotionVectorsId = Shader.PropertyToID("_ToonRP_ZeroMotionVectors");
        private static readonly int NonJitteredViewProjMatrixId = Shader.PropertyToID("_NonJitteredViewProjMatrix");
        private static readonly int MotionVectorsTextureId = Shader.PropertyToID("_ToonRP_MotionVectorsTexture");

        private readonly ToonDepthPrePass _depthPrePass;

        public ToonMotionVectorsPrePass(ToonDepthPrePass depthPrePass) => _depthPrePass = depthPrePass;

        public RenderTargetIdentifier MotionVectorsTexture { get; private set; }

        public void Render(ref RenderContext context)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            // This is required to compute previous object matrices
            context.Camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.MotionVectorsPrePass)))
            {
                cmd.SetGlobalMatrix(PrevViewProjMatrixId, context.MotionVectorsPersistentData.PreviousViewProjection);
                cmd.SetGlobalMatrix(NonJitteredViewProjMatrixId, context.MotionVectorsPersistentData.ViewProjection);

                int rtWidth = context.RtWidth;
                int rtHeight = context.RtHeight;
                var desc = new RenderTextureDescriptor(rtWidth, rtHeight,
                    GraphicsFormat.R16G16_SFloat, 0
                );
                MotionVectorsTexture = GetTemporaryRT(cmd, context.AdditionalCameraData, MotionVectorsTextureId, desc,
                    FilterMode.Point
                );
                cmd.SetRenderTarget(
                    MotionVectorsTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    _depthPrePass.DepthTexture, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
                );
                cmd.ClearRenderTarget(false, true, Color.black);

                using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Zero Motion Vectors")))
                {
                    cmd.SetGlobalFloat(ZeroMotionVectorsId, 1.0f);
                    context.Srp.ExecuteCommandBufferAndClear(cmd);
                    DrawRenderers(cmd, ref context, context.Settings.MotionVectorsZeroLayerMask);
                }

                cmd.SetGlobalFloat(ZeroMotionVectorsId, 0.0f);
                context.Srp.ExecuteCommandBufferAndClear(cmd);
                DrawRenderers(cmd, ref context, ~context.Settings.MotionVectorsZeroLayerMask);
            }

            context.Srp.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Cleanup(ref ScriptableRenderContext context)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.ReleaseTemporaryRT(MotionVectorsTextureId);
            context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void DrawRenderers(CommandBuffer cmd, ref RenderContext context, int extraLayerMask)
        {
            Camera camera = context.Camera;

            var sortingSettings = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonOpaque,
            };
            var drawingSettings = new DrawingSettings(MotionVectorsShaderTagId, sortingSettings)
            {
                enableDynamicBatching = context.Settings.UseDynamicBatching,
                perObjectData = PerObjectData.MotionVectors,
            };

            int layerMask = camera.cullingMask & context.Settings.OpaqueLayerMask & extraLayerMask;
            if (layerMask == 0)
            {
                return;
            }

            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask)
            {
                excludeMotionVectorObjects = true,
            };

            var renderStateBlock = new RenderStateBlock(RenderStateMask.Depth)
            {
                depthState = new DepthState { writeEnabled = true, compareFunction = CompareFunction.LessEqual },
            };

            context.Srp.DrawRenderers(context.CullingResults,
                ref drawingSettings, ref filteringSettings, ref renderStateBlock
            );

            context.ExtensionsCollection.OnPrePass(PrePassMode.MotionVectors,
                ref context.Srp, cmd,
                ref drawingSettings, ref filteringSettings, ref renderStateBlock
            );
        }

        public struct RenderContext
        {
            public ScriptableRenderContext Srp;
            public CullingResults CullingResults;

            public readonly Camera Camera;
            public readonly ToonAdditionalCameraData AdditionalCameraData;
            public readonly ToonMotionVectorsPersistentData MotionVectorsPersistentData;
            public readonly ToonCameraRendererSettings Settings;
            public readonly ToonRenderingExtensionsCollection ExtensionsCollection;

            public readonly int RtWidth;
            public readonly int RtHeight;

            public RenderContext(ScriptableRenderContext srp, CullingResults cullingResults, Camera camera,
                ToonAdditionalCameraData additionalCameraData, ToonCameraRendererSettings settings,
                ToonRenderingExtensionsCollection extensionsCollection, int rtWidth, int rtHeight)
            {
                Srp = srp;
                CullingResults = cullingResults;
                Camera = camera;
                AdditionalCameraData = additionalCameraData;
                MotionVectorsPersistentData = additionalCameraData.GetPersistentData<ToonMotionVectorsPersistentData>();
                Settings = settings;
                ExtensionsCollection = extensionsCollection;
                RtWidth = rtWidth;
                RtHeight = rtHeight;
            }
        }
    }
}