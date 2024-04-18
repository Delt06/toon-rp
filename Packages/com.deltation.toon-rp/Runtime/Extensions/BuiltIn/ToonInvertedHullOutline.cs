using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static DELTation.ToonRP.Extensions.BuiltIn.ToonInvertedHullOutlineSettings;
using static DELTation.ToonRP.ToonCameraRenderer;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    public sealed class ToonInvertedHullOutline : ToonRenderingExtensionBase
    {
        private const int DepthOnlyPass = 1;
        private const int DepthNormalsPass = 2;
        private const int MotionVectorsPass = 3;

        public const string DefaultShaderName = "Hidden/Toon RP/Outline (Inverted Hull)";

        private readonly List<ToonPipelineMaterial> _materials = new();
        private ToonAdditionalCameraData _additionalCameraData;

        private Camera _camera;
        private ToonCameraRendererSettings _cameraRendererSettings;
        private ToonCameraRenderTarget _cameraRenderTarget;
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;

        private ToonInvertedHullOutlineSettings _outlineSettings;

        public override void Dispose()
        {
            base.Dispose();

            foreach (ToonPipelineMaterial material in _materials)
            {
                material.Dispose();
            }

            _materials.Clear();
        }

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
        }

        private static void SetPassProperties(CommandBuffer cmd, in Pass pass, Material material)
        {
            cmd.SetGlobalFloat(ShaderIds.ThicknessId, pass.Thickness);
            cmd.SetGlobalVector(ShaderIds.ColorId, pass.Color);
            cmd.SetGlobalVector(ShaderIds.DistanceFadeId,
                new Vector4(
                    1.0f / pass.MaxDistance,
                    1.0f / pass.DistanceFade
                )
            );

            material.SetKeyword(ShaderKeywords.FixedScreenSpaceThicknessKeywordName, pass.FixedScreenSpaceThickness);

            bool noiseEnabled = pass.IsNoiseEnabled;
            material.SetKeyword(ShaderKeywords.NoiseKeywordName, noiseEnabled);
            if (noiseEnabled)
            {
                cmd.SetGlobalFloat(ShaderIds.NoiseFrequencyId, pass.NoiseFrequency);
                cmd.SetGlobalFloat(ShaderIds.NoiseAmplitudeId, pass.NoiseAmplitude);
            }

            {
                material.SetKeyword(ShaderKeywords.NormalSemanticUV2KeywordName,
                    pass.NormalsSource == NormalsSource.UV2
                );
                material.SetKeyword(ShaderKeywords.NormalSemanticTangentKeywordName,
                    pass.NormalsSource == NormalsSource.Tangents
                );
            }

            material.SetKeyword(ShaderKeywords.DistanceFadeKeywordName, pass.IsDistanceFadeEnabled);
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

            PopulateMaterialsForAllPasses();

            int originalLayerMask = filteringSettings.layerMask;

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
                        filteringSettings.layerMask = originalLayerMask & pass.LayerMask;
                        if (filteringSettings.layerMask == 0)
                        {
                            continue;
                        }
                        
                        Material material = _materials[passIndex].GetOrCreate();
                        SetPassProperties(cmd, pass, material);

                        cmd.SetGlobalDepthBias(pass.DepthBias, 0);
                        cameraOverride.OverrideIfEnabled(cmd, pass.CameraOverrides);
                        context.ExecuteCommandBufferAndClear(cmd);

                        drawingSettings.overrideMaterial = material;

                        for (int i = 0; i < ShaderTagIds.Length; i++)
                        {
                            drawingSettings.SetShaderPassName(i, ShaderTagIds[i]);
                        }

                        renderStateBlock.mask |= RenderStateMask.Raster | RenderStateMask.Stencil;
                        renderStateBlock.rasterState = new RasterState(CullMode.Front, 0, pass.DepthBias);

                        if (pass.StencilLayer != StencilLayer.None)
                        {
                            byte reference = pass.StencilLayer.ToReference();
                            renderStateBlock.stencilReference = reference;
                            renderStateBlock.stencilState =
                                new StencilState(true, reference, 255, CompareFunction.NotEqual, pass.StencilPassOp);
                        }
                        else if (pass.StencilPassOp != StencilOp.Keep)
                        {
                            renderStateBlock.stencilState = new StencilState(true, 0, 255, CompareFunction.Always,
                                pass.StencilPassOp
                            );
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
            Pass[] passes = _outlineSettings.Passes;

            while (_materials.Count < passes.Length)
            {
                _materials.Add(null);
            }

            for (int passIndex = 0; passIndex < passes.Length; passIndex++)
            {
                ToonPipelineMaterial existingMaterial = _materials[passIndex];
                ref readonly Pass pass = ref passes[passIndex];
                Material overrideMaterial = pass.OverrideMaterial;
                if (existingMaterial == null ||
                    overrideMaterial != null && overrideMaterial.shader != existingMaterial.Shader ||
                    overrideMaterial == null && existingMaterial.ShaderName != DefaultShaderName)
                {
                    _materials[passIndex] = CreateMaterial(pass);
                }
            }
        }

        private static ToonPipelineMaterial CreateMaterial(in Pass pass)
        {
            const string materialName = "Toon RP Outline (Inverted Hull)";
            return pass.OverrideMaterial != null
                ? new ToonPipelineMaterial(pass.OverrideMaterial, materialName)
                : new ToonPipelineMaterial(DefaultShaderName, materialName);
        }

        public static class ShaderKeywords
        {
            public const string NoiseKeywordName = "_NOISE";
            public const string FixedScreenSpaceThicknessKeywordName = "_FIXED_SCREEN_SPACE_THICKNESS";
            public const string NormalSemanticUV2KeywordName = "_NORMAL_SEMANTIC_UV2";
            public const string NormalSemanticTangentKeywordName = "_NORMAL_SEMANTIC_TANGENT";
            public const string DistanceFadeKeywordName = "_DISTANCE_FADE";
        }

        private static class ShaderIds
        {
            public static readonly int NoiseAmplitudeId =
                Shader.PropertyToID("_ToonRpInvertedHullOutline_NoiseAmplitude");
            public static readonly int ThicknessId =
                Shader.PropertyToID("_ToonRpInvertedHullOutline_Thickness");
            public static readonly int DistanceFadeId =
                Shader.PropertyToID("_ToonRpInvertedHullOutline_DistanceFade");
            public static readonly int ColorId =
                Shader.PropertyToID("_ToonRpInvertedHullOutline_Color");
            public static readonly int NoiseFrequencyId =
                Shader.PropertyToID("_ToonRpInvertedHullOutline_NoiseFrequency");
        }
    }
}