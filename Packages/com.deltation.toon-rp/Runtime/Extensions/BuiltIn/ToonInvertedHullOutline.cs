using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static DELTation.ToonRP.Extensions.BuiltIn.ToonInvertedHullOutlineSettings;
using static DELTation.ToonRP.ToonCameraRenderer;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    public class ToonInvertedHullOutline : ToonRenderingExtensionBase
    {
        public const int DepthOnlyPass = 1;
        public const int DepthNormalsPass = 2;
        public const int MotionVectorsPass = 3;

        public const string ShaderName = "Hidden/Toon RP/Outline (Inverted Hull)";
        public const string NoiseKeywordName = "_NOISE";
        public const string VertexColorThicknessRKeywordName = "_VERTEX_COLOR_THICKNESS_R";
        public const string VertexColorThicknessGKeywordName = "_VERTEX_COLOR_THICKNESS_G";
        public const string VertexColorThicknessBKeywordName = "_VERTEX_COLOR_THICKNESS_B";
        public const string VertexColorThicknessAKeywordName = "_VERTEX_COLOR_THICKNESS_A";
        public const string FixedScreenSpaceThicknessKeywordName = "_FIXED_SCREEN_SPACE_THICKNESS";
        public const string NormalSemanticUV2KeywordName = "_NORMAL_SEMANTIC_UV2";
        public const string NormalSemanticTangentKeywordName = "_NORMAL_SEMANTIC_TANGENT";
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
        private ToonCameraRenderTarget _cameraRenderTarget;
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;

        private ToonInvertedHullOutlineSettings _outlineSettings;

        public override bool ShouldRender(in ToonRenderingExtensionContext context) => IsGameOrSceneView(context);

        public override void Setup(in ToonRenderingExtensionContext context,
            IToonRenderingExtensionSettingsStorage settingsStorage)
        {
            _cameraRendererSettings = context.CameraRendererSettings;
            _camera = context.Camera;
            _context = context.ScriptableRenderContext;
            _cullingResults = context.CullingResults;
            _cameraRenderTarget = context.CameraRenderTarget;
            _outlineSettings = settingsStorage.GetSettings<ToonInvertedHullOutlineSettings>(this);
            _additionalCameraData = context.AdditionalCameraData;

            PopulateMaterialsForAllPasses();
            UpdateMaterials();
        }

        private void UpdateMaterials()
        {
            for (int passIndex = 0; passIndex < _outlineSettings.Passes.Length; passIndex++)
            {
                ref readonly Pass pass = ref _outlineSettings.Passes[passIndex];
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
                    material.SetKeyword(VertexColorThicknessRKeywordName,
                        pass.VertexColorThicknessSource == VertexColorThicknessSource.R
                    );
                    material.SetKeyword(VertexColorThicknessGKeywordName,
                        pass.VertexColorThicknessSource == VertexColorThicknessSource.G
                    );
                    material.SetKeyword(VertexColorThicknessBKeywordName,
                        pass.VertexColorThicknessSource == VertexColorThicknessSource.B
                    );
                    material.SetKeyword(VertexColorThicknessAKeywordName,
                        pass.VertexColorThicknessSource == VertexColorThicknessSource.A
                    );
                }

                {
                    material.SetKeyword(NormalSemanticUV2KeywordName,
                        pass.NormalsSource == NormalsSource.UV2
                    );
                    material.SetKeyword(NormalSemanticTangentKeywordName,
                        pass.NormalsSource == NormalsSource.Tangents
                    );
                }

                material.SetKeyword(DistanceFadeKeywordName, pass.IsDistanceFadeEnabled);
            }
        }

        public override void OnPrePass(PrePassMode prePassMode, ref ScriptableRenderContext context,
            CommandBuffer cmd,
            ref DrawingSettings drawingSettings,
            ref FilteringSettings filteringSettings, ref RenderStateBlock renderStateBlock)
        {
            if (_outlineSettings.Passes.Length == 0)
            {
                return;
            }

            const int unknownPassIndex = -1;

            int passIndex = prePassMode switch
            {
                PrePassMode.Depth => DepthOnlyPass,
                PrePassMode.Depth | PrePassMode.Normals => DepthNormalsPass,
                PrePassMode.MotionVectors => MotionVectorsPass,
                var _ => unknownPassIndex,
            };
            if (passIndex == unknownPassIndex)
            {
                return;
            }

            // Because an override material is used, light mode will be ignored.
            // Thus, we have to manually specify the pass.
            drawingSettings.overrideMaterialPassIndex = passIndex;
            Render(cmd, ref context, ref drawingSettings, ref filteringSettings, ref renderStateBlock, prePassMode);
        }

        public override void Render()
        {
            if (_outlineSettings.Passes.Length == 0)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            var sortingSettings = new SortingSettings(_camera)
            {
                criteria = SortingCriteria.CommonOpaque,
            };
            var drawingSettings = new DrawingSettings(ShaderTagIds[0], sortingSettings)
            {
                enableDynamicBatching = _cameraRendererSettings.UseDynamicBatching,
            };
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
            var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            Render(cmd, ref _context, ref drawingSettings, ref filteringSettings, ref renderStateBlock);

            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref ScriptableRenderContext context,
            ref DrawingSettings drawingSettings,
            ref FilteringSettings filteringSettings, ref RenderStateBlock renderStateBlock,
            PrePassMode? prePassMode = null)
        {
            var cameraOverride =
                new ToonCameraOverride(_camera, _additionalCameraData, _cameraRenderTarget, prePassMode);

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.InvertedHullOutlines)))
            {
                context.ExecuteCommandBufferAndClear(cmd);

                for (int passIndex = 0; passIndex < _outlineSettings.Passes.Length; passIndex++)
                {
                    ref readonly Pass pass = ref _outlineSettings.Passes[passIndex];

                    if (prePassMode.HasValue && pass.PrePassIgnoreMask.Includes(prePassMode.Value))
                    {
                        continue;
                    }

                    string passName = string.IsNullOrWhiteSpace(pass.Name) ? "Unnamed Outline Pass" : pass.Name;
                    using (new ProfilingScope(cmd, NamedProfilingSampler.Get(passName)))
                    {
                        cmd.SetGlobalDepthBias(pass.DepthBias, 0);
                        cameraOverride.OverrideIfEnabled(cmd, pass.CameraOverrides);
                        context.ExecuteCommandBufferAndClear(cmd);

                        drawingSettings.overrideMaterial = _materials[passIndex];

                        for (int i = 0; i < ShaderTagIds.Length; i++)
                        {
                            drawingSettings.SetShaderPassName(i, ShaderTagIds[i]);
                        }

                        filteringSettings.layerMask = pass.LayerMask;

                        renderStateBlock.mask |= RenderStateMask.Raster | RenderStateMask.Stencil;
                        renderStateBlock.rasterState = new RasterState(CullMode.Front, 0, pass.DepthBias);

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

                        context.DrawRenderers(_cullingResults,
                            ref drawingSettings, ref filteringSettings, ref renderStateBlock
                        );

                        cmd.SetGlobalDepthBias(0, 0);
                        cameraOverride.Restore(cmd);
                    }
                }
            }

            context.ExecuteCommandBufferAndClear(cmd);
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