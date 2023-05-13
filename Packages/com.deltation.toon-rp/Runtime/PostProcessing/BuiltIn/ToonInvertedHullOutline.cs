using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static DELTation.ToonRP.ToonCameraRenderer;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    public class ToonInvertedHullOutline
    {
        private const int DefaultPassId = 0;
        private const int UvNormalsPassId = 1;
        private const int TangentNormalsPassId = 2;
        private static readonly int ThicknessId = Shader.PropertyToID("_Thickness");
        private static readonly int DistanceFadeId = Shader.PropertyToID("_DistanceFade");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int NoiseFrequencyId = Shader.PropertyToID("_NoiseFrequency");
        private static readonly int NoiseAmplitudeId = Shader.PropertyToID("_NoiseAmplitude");
        private readonly List<Material> _materials = new();
        private Camera _camera;
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;
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
            EnsureMaterialsAreCreated();
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

                for (int passIndex = 0; passIndex < _outlineSettings.Passes.Length; passIndex++)
                {
                    ToonInvertedHullOutlineSettings.Pass pass = _outlineSettings.Passes[passIndex];
                    string passName = string.IsNullOrWhiteSpace(pass.Name) ? "Unnamed Outline Pass" : pass.Name;
                    using (new ProfilingScope(cmd, NamedProfilingSampler.Get(passName)))
                    {
                        Material material = _materials[passIndex];
                        material.SetFloat(ThicknessId, pass.Thickness);
                        material.SetVector(ColorId, pass.Color);
                        material.SetVector(DistanceFadeId,
                            new Vector4(
                                1.0f / pass.MaxDistance,
                                1.0f / pass.DistanceFade
                            )
                        );

                        bool noiseEnabled = pass.NoiseAmplitude > 0.0f && pass.NoiseFrequency > 0.0f;
                        material.SetKeyword("_NOISE", noiseEnabled);
                        if (noiseEnabled)
                        {
                            material.SetFloat(NoiseFrequencyId, pass.NoiseFrequency);
                            material.SetFloat(NoiseAmplitudeId, pass.NoiseAmplitude);
                        }

                        material.SetKeyword("_DISTANCE_FADE", pass.MaxDistance > 0.0f);

                        cmd.SetGlobalDepthBias(pass.DepthBias, 0);
                        ExecuteBuffer(cmd);

                        var sortingSettings = new SortingSettings(_camera)
                        {
                            criteria = SortingCriteria.CommonOpaque,
                        };
                        var drawingSettings = new DrawingSettings(ShaderTagIds[0], sortingSettings)
                        {
                            enableDynamicBatching = _settings.UseDynamicBatching,
                            overrideMaterial = material,
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

        private void EnsureMaterialsAreCreated()
        {
            while (_materials.Count < _outlineSettings.Passes.Length)
            {
                _materials.Add(CreateMaterial());
            }

            for (int i = 0; i < _materials.Count; i++)
            {
                _materials[i] ??= CreateMaterial();
            }
        }

        private static Material CreateMaterial()
        {
            var shader = Shader.Find("Hidden/Toon RP/Outline (Inverted Hull)");
            return new Material(shader)
            {
                name = "Toon RP Outline (Inverted Hull)",
            };
        }
    }
}