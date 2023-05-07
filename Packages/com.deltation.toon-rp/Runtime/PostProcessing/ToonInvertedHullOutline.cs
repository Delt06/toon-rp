using System;
using UnityEngine;
using UnityEngine.Rendering;
using static DELTation.ToonRP.ToonCameraRenderer;

namespace DELTation.ToonRP.PostProcessing
{
    public class ToonInvertedHullOutline
    {
        private const int DefaultPassId = 0;
        private const int UvNormalsPassId = 1;
        private const int TangentNormalsPassId = 2;
        private static readonly int ThicknessId = Shader.PropertyToID("_ToonRP_Outline_InvertedHull_Thickness");
        private static readonly int DistanceFadeId = Shader.PropertyToID("_ToonRP_Outline_DistanceFade");
        private static readonly int ColorId = Shader.PropertyToID("_ToonRP_Outline_InvertedHull_Color");
        private static readonly int NoiseFrequencyId = Shader.PropertyToID("_ToonRP_Outline_NoiseFrequency");
        private static readonly int NoiseAmplitudeId = Shader.PropertyToID("_ToonRP_Outline_NoiseAmplitude");
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

            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.InvertedHullOutlines)))
            {
                ExecuteBuffer(cmd);

                foreach (ToonInvertedHullOutlineSettings.Pass pass in _outlineSettings.Passes)
                {
                    string passName = string.IsNullOrWhiteSpace(pass.Name) ? "Unnamed Outline Pass" : pass.Name;
                    using (new ProfilingScope(cmd, NamedProfilingSampler.Get(passName)))
                    {
                        cmd.SetGlobalFloat(ThicknessId, pass.Thickness);
                        cmd.SetGlobalVector(ColorId, pass.Color);
                        cmd.SetGlobalDepthBias(pass.DepthBias, 0);
                        cmd.SetGlobalVector(DistanceFadeId,
                            new Vector4(
                                1.0f / pass.MaxDistance,
                                1.0f / pass.DistanceFade
                            )
                        );
                        cmd.SetGlobalFloat(NoiseFrequencyId, pass.NoiseFrequency);
                        cmd.SetGlobalFloat(NoiseAmplitudeId, pass.NoiseAmplitude);
                        ExecuteBuffer(cmd);

                        var sortingSettings = new SortingSettings(_camera)
                        {
                            criteria = SortingCriteria.CommonOpaque,
                        };
                        var drawingSettings = new DrawingSettings(ShaderTagIds[0], sortingSettings)
                        {
                            enableDynamicBatching = _settings.UseDynamicBatching,
                            overrideMaterial = _outlineMaterial,
                            overrideMaterialPassIndex = pass.NormalsSource switch
                            {
                                ToonInvertedHullOutlineSettings.NormalsSource.Normals => DefaultPassId,
                                ToonInvertedHullOutlineSettings.NormalsSource.UV2 => UvNormalsPassId,
                                ToonInvertedHullOutlineSettings.NormalsSource.Tangents => TangentNormalsPassId,
                                _ => throw new ArgumentOutOfRangeException(),
                            },
                        };

                        for (int i = 0; i < ShaderTagIds.Length; i++)
                        {
                            drawingSettings.SetShaderPassName(i, ShaderTagIds[i]);
                        }

                        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque)
                        {
                            layerMask = pass.LayerMask,
                        };
                        var renderStateBlock = new RenderStateBlock
                        {
                            mask = RenderStateMask.Raster | RenderStateMask.Stencil,
                            rasterState = new RasterState(CullMode.Front, 0, pass.DepthBias),
                        };
                        if (pass.StencilLayer != StencilLayer.None)
                        {
                            byte reference = pass.StencilLayer.ToReference();
                            renderStateBlock.stencilReference = reference;
                            renderStateBlock.stencilState =
                                new StencilState(true, reference, 0, CompareFunction.NotEqual);
                        }
                        else
                        {
                            renderStateBlock.stencilState = new StencilState(false);
                        }


                        _context.DrawRenderers(_cullingResults,
                            ref drawingSettings, ref filteringSettings, ref renderStateBlock
                        );


                        cmd.SetGlobalDepthBias(0, 0);
                        ExecuteBuffer(cmd);
                    }
                }
            }

            ExecuteBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

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