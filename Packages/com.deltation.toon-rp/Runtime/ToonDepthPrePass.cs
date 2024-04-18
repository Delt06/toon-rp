using DELTation.ToonRP.Extensions;
using JetBrains.Annotations;
using UnityEngine;
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

        [CanBeNull]
        private ToonCopyDepth _copyDepth;
        private bool _normals;

        public ToonDepthPrePass() : this(DepthTextureId, NormalsTextureId) { }

        public ToonDepthPrePass(int depthTextureId, int normalsTextureId)
        {
            _depthTextureId = depthTextureId;
            _normalsTextureId = normalsTextureId;
        }

        public bool UseCopyDepth { get; private set; }

        public RenderTargetIdentifier DepthTexture { get; private set; }
        public RenderTargetIdentifier NormalsTexture { get; private set; }

        public void ConfigureCopyDepth(bool copyDepth)
        {
            UseCopyDepth = copyDepth;
        }

        public void Render(ref RenderContext context)
        {
            _normals = context.Mode.Includes(PrePassMode.Normals);

            CommandBuffer cmd = CommandBufferPool.Get();

            GraphicsFormat depthStencilFormat = ToonFormatUtils.GetDefaultDepthFormat(context.Stencil);

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.DepthPrePass)))
            {
                GetDepthRT(cmd, context.AdditionalCameraData, context.RtWidth, context.RtHeight, depthStencilFormat);
                if (_normals)
                {
                    var normalsDesc = new RenderTextureDescriptor(context.RtWidth, context.RtHeight,
                        RenderTextureFormat.RG16, 0
                    );
                    NormalsTexture = GetTemporaryRT(cmd, context.AdditionalCameraData, _normalsTextureId, normalsDesc,
                        FilterMode.Point
                    );
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

                context.Srp.ExecuteCommandBufferAndClear(cmd);

                DrawRenderers(cmd, ref context);
            }

            context.Srp.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void GetDepthRT(CommandBuffer cmd, ToonAdditionalCameraData additionalCameraData, int width, int height,
            GraphicsFormat format)
        {
            var depthDesc = new RenderTextureDescriptor(width, height,
                GraphicsFormat.None, format,
                0
            );
            DepthTexture = GetTemporaryRT(cmd, additionalCameraData, _depthTextureId, depthDesc, FilterMode.Point);
        }

        public void CopyDepth(CommandBuffer cmd, ref CopyContext context)
        {
            _normals = false;

            ToonCameraRenderTarget renderTarget = context.RenderTarget;
            GraphicsFormat format = renderTarget.DepthStencilFormat;
            GetDepthRT(cmd, context.AdditionalCameraData, renderTarget.Width, renderTarget.Height, format);

            _copyDepth ??= new ToonCopyDepth();

            const bool renderToTexture = true;
            const bool setupViewport = false;
            var copyContext =
                new ToonCopyDepth.CopyContext(context.Camera, renderTarget, renderToTexture, setupViewport);
            _copyDepth.Copy(cmd, copyContext, renderTarget.CurrentDepthBufferId(), DepthTexture);

            context.Srp.ExecuteCommandBufferAndClear(cmd);
        }

        public void Cleanup(ref ScriptableRenderContext context)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.ReleaseTemporaryRT(_depthTextureId);
            if (_normals)
            {
                cmd.ReleaseTemporaryRT(_normalsTextureId);
            }

            context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void DrawRenderers(CommandBuffer cmd, ref RenderContext context)
        {
            Camera camera = context.Camera;

            var sortingSettings = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonOpaque,
            };
            ShaderTagId shaderPassName = _normals ? DepthNormalsShaderTagId : DepthOnlyShaderTagId;
            var drawingSettings = new DrawingSettings(shaderPassName, sortingSettings)
            {
                enableDynamicBatching = context.Settings.UseDynamicBatching,
            };

            int layerMask = camera.cullingMask & context.Settings.OpaqueLayerMask;
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);
            var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            context.Srp.DrawRenderers(context.CullingResults,
                ref drawingSettings, ref filteringSettings, ref renderStateBlock
            );

            context.ExtensionsCollection.OnPrePass(
                _normals ? PrePassMode.Normals | PrePassMode.Depth : PrePassMode.Depth,
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
            public readonly ToonCameraRendererSettings Settings;
            public readonly ToonRenderingExtensionsCollection ExtensionsCollection;

            public readonly PrePassMode Mode;
            public readonly int RtWidth;
            public readonly int RtHeight;
            public readonly bool Stencil;

            public RenderContext(ScriptableRenderContext srp, CullingResults cullingResults, Camera camera,
                ToonAdditionalCameraData additionalCameraData, ToonCameraRendererSettings settings,
                ToonRenderingExtensionsCollection extensionsCollection, PrePassMode mode, int rtWidth, int rtHeight,
                bool stencil = false)
            {
                Srp = srp;
                CullingResults = cullingResults;
                Camera = camera;
                AdditionalCameraData = additionalCameraData;
                Settings = settings;
                ExtensionsCollection = extensionsCollection;
                Mode = mode;
                RtWidth = rtWidth;
                RtHeight = rtHeight;
                Stencil = stencil;
            }
        }

        public struct CopyContext
        {
            public ScriptableRenderContext Srp;
            public readonly Camera Camera;
            public readonly ToonAdditionalCameraData AdditionalCameraData;
            public readonly ToonCameraRenderTarget RenderTarget;

            public CopyContext(ScriptableRenderContext srp, Camera camera,
                ToonAdditionalCameraData additionalCameraData, ToonCameraRenderTarget renderTarget)
            {
                Srp = srp;
                Camera = camera;
                AdditionalCameraData = additionalCameraData;
                RenderTarget = renderTarget;
            }
        }
    }
}