using UnityEngine;
using UnityEngine.Rendering;

namespace ToonRP.Runtime.PostProcessing
{
    public class ToonInvertedHullOutline
    {
        private const string SampleName = "Outline (Inverted Hull)";

        private const int DefaultPassId = 0;
        private const int UvNormalsPassId = 1;
        private static readonly int ThicknessId = Shader.PropertyToID("_ToonRP_Outline_InvertedHull_Thickness");
        private static readonly int DistanceFadeId = Shader.PropertyToID("_ToonRP_Outline_DistanceFade");
        private static readonly int ColorId = Shader.PropertyToID("_ToonRP_Outline_InvertedHull_Color");
        private readonly CommandBuffer _cmd = new() { name = SampleName };
        private Camera _camera;
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;
        private Material _outlineMaterial;
        private ToonInvertedHullOutlineSettings _outlineSettings;
        private ToonCameraRendererSettings _settings;

        public void Setup(in ScriptableRenderContext context,
            in CullingResults cullingResults,
            Camera camera,
            in ToonCameraRendererSettings settings,
            in ToonInvertedHullOutlineSettings outlineSettings)
        {
            _settings = settings;
            _camera = camera;
            _context = context;
            _cullingResults = cullingResults;
            _outlineSettings = outlineSettings;
            EnsureMaterialIsCreated();
        }

        public void Render()
        {
            if (_outlineSettings.Passes.Length == 0)
            {
                return;
            }

            _cmd.BeginSample(SampleName);
            ExecuteBuffer();

            foreach (ToonInvertedHullOutlineSettings.Pass pass in _outlineSettings.Passes)
            {
                _cmd.SetGlobalFloat(ThicknessId, pass.Thickness);
                _cmd.SetGlobalVector(ColorId, pass.Color);
                _cmd.SetGlobalDepthBias(pass.DepthBias, 0);
                _cmd.SetGlobalVector(DistanceFadeId,
                    new Vector4(
                        1.0f / pass.MaxDistance,
                        1.0f / pass.DistanceFade
                    )
                );
                ExecuteBuffer();

                var sortingSettings = new SortingSettings(_camera)
                {
                    criteria = SortingCriteria.CommonOpaque,
                };
                var drawingSettings = new DrawingSettings(ToonCameraRenderer.ForwardShaderTagId, sortingSettings)
                {
                    enableDynamicBatching = _settings.UseDynamicBatching,
                    overrideMaterial = _outlineMaterial,
                    overrideMaterialPassIndex = pass.UseNormalsFromUV2 ? UvNormalsPassId : DefaultPassId,
                };
                var filteringSettings = new FilteringSettings(RenderQueueRange.opaque)
                {
                    layerMask = pass.LayerMask,
                };
                var renderStateBlock = new RenderStateBlock
                {
                    mask = RenderStateMask.Raster,
                    rasterState = new RasterState(CullMode.Front, 0, pass.DepthBias),
                };

                _context.DrawRenderers(_cullingResults,
                    ref drawingSettings, ref filteringSettings, ref renderStateBlock
                );


                _cmd.SetGlobalDepthBias(0, 0);
                ExecuteBuffer();
            }

            _cmd.EndSample(SampleName);
            ExecuteBuffer();
        }

        private void ExecuteBuffer() => ExecuteBuffer(_cmd);

        private void ExecuteBuffer(CommandBuffer cmd)
        {
            _context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        private void EnsureMaterialIsCreated()
        {
            if (_outlineMaterial != null)
            {
                return;
            }

            var shader = Shader.Find("Hidden/Toon RP/Outline (Inverted Hull)");
            _outlineMaterial = new Material(shader)
            {
                name = "Toon RP Outline (Inverted Hull)",
            };
        }
    }
}