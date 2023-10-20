using System;
using DELTation.ToonRP.Editor.ShaderGUI;
using DELTation.ToonRP.Editor.ShaderGUI.ShaderGraph;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEngine;
using static DELTation.ToonRP.Editor.ToonShaderUtils;

namespace DELTation.ToonRP.Editor.ShaderGraph.Targets
{
    internal sealed class ToonUnlitSubTarget : ToonSubTarget
    {
        private static readonly GUID SourceCodeGuid = new("abf23a4e770c34d48971749133b9e4a5"); // ToonUnlitSubTarget.cs

        public ToonUnlitSubTarget() => displayName = "Unlit";

        protected override ShaderID ShaderID => ShaderID.Unlit;

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(SourceCodeGuid, AssetCollection.Flags.SourceDependency);
            base.Setup(ref context);

            Type toonRpType = typeof(ToonRenderPipelineAsset);
            if (!context.HasCustomEditorForRenderPipeline(toonRpType))
            {
                Type gui = typeof(ToonRpShaderGraphUnlitShaderGui);
                context.AddCustomEditorForRenderPipeline(gui.FullName, toonRpType);
            }

            // Process SubShaders
            context.AddSubShader(PostProcessSubShader(SubShaders.Unlit(target, target.RenderType, target.RenderQueue)));
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            if (target.AllowMaterialOverride)
            {
                // copy our target's default settings into the material
                // (technically not necessary since we are always recreating the material from the shader each time,
                // which will pull over the defaults from the shader definition)
                // but if that ever changes, this will ensure the defaults are set
                material.SetFloat(PropertyNames.SurfaceType, (float) target.SurfaceType);
                material.SetFloat(PropertyNames.BlendMode, (float) target.AlphaMode);
                material.SetFloat(PropertyNames.AlphaClipping, target.AlphaClip ? 1.0f : 0.0f);
                material.SetFloat(PropertyNames.RenderFace, (int) target.RenderFace);
                material.SetFloat(PropertyNames.CastShadows, target.CastShadows ? 1.0f : 0.0f);
                material.SetFloat(PropertyNames.ZWriteControl, (float) target.ZWriteControl);
                material.SetFloat(PropertyNames.ZTest, (float) target.ZTestMode);
            }
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(ToonBlockFields.SurfaceDescription.Alpha,
                target.SurfaceType == SurfaceType.Transparent || target.AlphaClip || target.AllowMaterialOverride
            );
            context.AddBlock(ToonBlockFields.SurfaceDescription.AlphaClipThreshold,
                target.AlphaClip || target.AllowMaterialOverride
            );
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            if (target.AllowMaterialOverride)
            {
                collector.AddFloatProperty(PropertyNames.CastShadows, target.CastShadows ? 1.0f : 0.0f);

                collector.AddFloatProperty(PropertyNames.SurfaceType, (float) target.SurfaceType);
                collector.AddFloatProperty(PropertyNames.BlendMode, (float) target.AlphaMode);
                collector.AddFloatProperty(PropertyNames.AlphaClipping, target.AlphaClip ? 1.0f : 0.0f);
                collector.AddFloatProperty(PropertyNames.BlendSrc, 1.0f); // always set by material inspector
                collector.AddFloatProperty(PropertyNames.BlendDst, 0.0f); // always set by material inspector
                collector.AddToggleProperty(PropertyNames.ZWrite, target.SurfaceType == SurfaceType.Opaque);
                collector.AddFloatProperty(PropertyNames.ZWriteControl, (float) target.ZWriteControl);
                collector.AddFloatProperty(PropertyNames.ZTest, (float) target.ZTestMode
                ); // ztest mode is designed to directly pass as ztest
                collector.AddFloatProperty(PropertyNames.RenderFace, (float) target.RenderFace
                ); // render face enum is designed to directly pass as a cull mode
            }
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange,
            Action<string> registerUndo)
        {
            target.AddDefaultMaterialOverrideGUI(ref context, onChange, registerUndo);
            target.AddDefaultSurfacePropertiesGUI(ref context, onChange, registerUndo, false);
        }

        #region SubShader

        private static class SubShaders
        {
            public static SubShaderDescriptor Unlit(ToonTarget target, string renderType, string renderQueue)
            {
                var result = new SubShaderDescriptor
                {
                    pipelineTag = ToonRenderPipeline.PipelineTag,
                    renderType = renderType,
                    renderQueue = renderQueue,
                    generatesPreview = true,
                    passes = new PassCollection
                    {
                        UnlitPasses.Forward(target),
                    },
                };

                if (target.MayWriteDepth)
                {
                    result.passes.Add(CorePasses.DepthOnly(target));
                    result.passes.Add(CorePasses.DepthNormals(target));
                    result.passes.Add(CorePasses.MotionVectors(target));
                }
                
                if (target.CastShadows || target.AllowMaterialOverride)
                {
                    result.passes.Add(CorePasses.ShadowCaster(target));
                }

                return result;
            }
        }

        #endregion

        #region Pass

        private static class UnlitPasses
        {
            public static PassDescriptor Forward(ToonTarget target)
            {
                ref readonly ToonPasses.Pass pass = ref ToonPasses.Forward;
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = pass.Name,
                    referenceName = pass.ReferenceName,
                    useInPreview = true,

                    // Template
                    passTemplatePath = ToonTarget.UberTemplatePath,
                    sharedTemplateDirectories = ToonTarget.SharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentColorAlpha,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = UnlitRequiredFields.Unlit,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target),
                    pragmas = CorePragmas.Forward,
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection(),
                    includes = UnlitIncludes.Unlit,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common,
                };

                CorePasses.AddTargetSurfaceControlsToPass(ref result, target);

                return result;
            }

            #region RequiredFields

            private static class UnlitRequiredFields
            {
                public static readonly FieldCollection Unlit = new()
                {
                    StructFields.Varyings.positionWS,
                    StructFields.Varyings.normalWS,
                    StructFields.Varyings.viewDirectionWS,
                    ToonStructFields.Varyings.fogFactorAndVertexLight,
                };
            }

            #endregion
        }

        #endregion

        #region Includes

        private static class UnlitIncludes
        {
            private const string UnlitPass =
                "Packages/com.deltation.toon-rp/Editor/ShaderGraph/Includes/UnlitPass.hlsl";

            public static readonly IncludeCollection Unlit = new()
            {
                // Pre-graph
                CoreIncludes.CorePregraph,
                CoreIncludes.ShaderGraphPregraph,

                // Post-graph
                CoreIncludes.CorePostgraph,
                { UnlitPass, IncludeLocation.Postgraph },
            };
        }

        #endregion
    }
}