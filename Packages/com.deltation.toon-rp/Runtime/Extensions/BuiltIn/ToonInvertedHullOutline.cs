using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static DELTation.ToonRP.Extensions.BuiltIn.ToonInvertedHullOutlineSettings;
using static DELTation.ToonRP.ToonCameraRenderer;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    public class ToonInvertedHullOutline : ToonRenderingExtensionBase
    {
        private const int DefaultPassId = 0;
        private const int UvNormalsPassId = 1;
        private const int TangentNormalsPassId = 2;
        public const string ShaderName = "Hidden/Toon RP/Outline (Inverted Hull)";
        public const string NoiseKeywordName = "_NOISE";
        public const string VertexColorThicknessRKeywordName = "_VERTEX_COLOR_THICKNESS_R";
        public const string VertexColorThicknessGKeywordName = "_VERTEX_COLOR_THICKNESS_G";
        public const string VertexColorThicknessBKeywordName = "_VERTEX_COLOR_THICKNESS_B";
        public const string VertexColorThicknessAKeywordName = "_VERTEX_COLOR_THICKNESS_A";
        public const string FixedScreenSpaceThicknessKeywordName = "_FIXED_SCREEN_SPACE_THICKNESS";
        public const string DistanceFadeKeywordName = "_DISTANCE_FADE";
        private static readonly int ThicknessId = Shader.PropertyToID("_Thickness");
        private static readonly int DistanceFadeId = Shader.PropertyToID("_DistanceFade");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int NoiseFrequencyId = Shader.PropertyToID("_NoiseFrequency");
        private static readonly int NoiseAmplitudeId = Shader.PropertyToID("_NoiseAmplitude");
        private readonly List<Material> _materials = new();
        private ToonAdditionalCameraData _additionalCameraData;

        private Camera _camera;
        private ToonCameraRendererSettings _cameraRendererSettings;
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;

        private ToonInvertedHullOutlineSettings _outlineSettings;

        public override bool ShouldRender(in ToonRenderingExtensionContext context) => IsGameOrSceneView(context);

        public override void Render()
        {
            if (_outlineSettings.Passes.Length == 0)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            var cameraOverride = new ToonCameraOverride(_camera, _additionalCameraData);

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.InvertedHullOutlines)))
            {
                _context.ExecuteCommandBufferAndClear(cmd);

                for (int passIndex = 0; passIndex < _outlineSettings.Passes.Length; passIndex++)
                {
                    ref readonly Pass pass = ref _outlineSettings.Passes[passIndex];
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

                        material.SetKeyword(FixedScreenSpaceThicknessKeywordName, pass.FixedScreenSpaceThickness);

                        bool noiseEnabled = pass.IsNoiseEnabled;
                        material.SetKeyword(NoiseKeywordName, noiseEnabled);
                        if (noiseEnabled)
                        {
                            material.SetFloat(NoiseFrequencyId, pass.NoiseFrequency);
                            material.SetFloat(NoiseAmplitudeId, pass.NoiseAmplitude);
                        }

                        {
                            material.SetKeyword("_VERTEX_COLOR_THICKNESS_R",
                                pass.VertexColorThicknessSource == VertexColorThicknessSource.R
                            );
                            material.SetKeyword("_VERTEX_COLOR_THICKNESS_G",
                                pass.VertexColorThicknessSource == VertexColorThicknessSource.G
                            );
                            material.SetKeyword("_VERTEX_COLOR_THICKNESS_B",
                                pass.VertexColorThicknessSource == VertexColorThicknessSource.B
                            );
                            material.SetKeyword("_VERTEX_COLOR_THICKNESS_A",
                                pass.VertexColorThicknessSource == VertexColorThicknessSource.A
                            );
                        }

                        material.SetKeyword(DistanceFadeKeywordName, pass.IsDistanceFadeEnabled);

                        cmd.SetGlobalDepthBias(pass.DepthBias, 0);
                        cameraOverride.OverrideIfEnabled(cmd, pass.CameraOverrides);
                        _context.ExecuteCommandBufferAndClear(cmd);

                        var sortingSettings = new SortingSettings(_camera)
                        {
                            criteria = SortingCriteria.CommonOpaque,
                        };
                        var drawingSettings = new DrawingSettings(ShaderTagIds[0], sortingSettings)
                        {
                            enableDynamicBatching = _cameraRendererSettings.UseDynamicBatching,
                            overrideMaterial = material,
                            overrideMaterialPassIndex = pass.NormalsSource switch
                            {
                                NormalsSource.Normals => DefaultPassId,
                                NormalsSource.UV2 => UvNormalsPassId,
                                NormalsSource.Tangents => TangentNormalsPassId,
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
                        cameraOverride.RestoreIfEnabled(cmd);
                    }
                }
            }

            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void Setup(in ToonRenderingExtensionContext context,
            IToonRenderingExtensionSettingsStorage settingsStorage)
        {
            _cameraRendererSettings = context.CameraRendererSettings;
            _camera = context.Camera;
            _context = context.ScriptableRenderContext;
            _cullingResults = context.CullingResults;
            _outlineSettings = settingsStorage.GetSettings<ToonInvertedHullOutlineSettings>(this);
            _additionalCameraData = context.AdditionalCameraData;
            PopulateMaterialsForAllPasses();
        }

        private void PopulateMaterialsForAllPasses()
        {
            while (_materials.Count < _outlineSettings.Passes.Length)
            {
                _materials.Add(CreateMaterial());
            }

            for (int i = 0; i < _materials.Count; i++)
            {
                if (_materials[i] == null)
                {
                    _materials[i] = CreateMaterial();
                }
            }
        }

        private static Material CreateMaterial() =>
            ToonRpUtils.CreateEngineMaterial(ShaderName, "Toon RP Outline (Inverted Hull)");
    }
}