using System;
using DELTation.ToonRP.Editor.ShaderGUI.ShaderGraph;
using DELTation.ToonRP.Extensions.BuiltIn;
using UnityEditor;
using UnityEditor.ShaderGraph;

namespace DELTation.ToonRP.Editor.ShaderGraph.Targets
{
    internal sealed class ToonInvertedHullOutlineSubTarget : ToonSubTarget
    {
        private static readonly GUID
            SourceCodeGuid = new("bced85488ec741ac8fb29aac653bda11"); // ToonInvertedHullOutlineSubTarget.cs

        public ToonInvertedHullOutlineSubTarget() => displayName = "Inverted Hull Outline";

        protected override ToonShaderUtils.ShaderID ShaderID => ToonShaderUtils.ShaderID.InvertedHullOutline;

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(SourceCodeGuid, AssetCollection.Flags.SourceDependency);
            base.Setup(ref context);

            Type toonRpType = typeof(ToonRenderPipelineAsset);
            if (!context.HasCustomEditorForRenderPipeline(toonRpType))
            {
                Type gui = typeof(ToonRpShaderGraphInvertedHullOutlineShaderGui);
                context.AddCustomEditorForRenderPipeline(gui.FullName, toonRpType);
            }

            target.SurfaceType = SurfaceType.Opaque;
            target.AlphaClip = false;
            target.ControlOutlinesStencilLayer = false;

            // Process SubShaders
            context.AddSubShader(
                PostProcessSubShader(SubShaders.InvertedHullOutline(target, target.RenderType, target.RenderQueueString)
                )
            );
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange,
            Action<string> registerUndo)
        {
            target.AddDefaultMaterialOverrideGUI(ref context, onChange, registerUndo);
            target.AddDefaultFogProperties(ref context, onChange, registerUndo);
        }

        #region SubShader

        private static class SubShaders
        {
            public static SubShaderDescriptor InvertedHullOutline(ToonTarget target, string renderType,
                string renderQueue)
            {
                var result = new SubShaderDescriptor
                {
                    pipelineTag = ToonRenderPipeline.PipelineTag,
                    renderType = renderType,
                    renderQueue = renderQueue,
                    generatesPreview = true,
                    passes = new PassCollection
                    {
                        Passes.Forward(target),
                        CorePasses.DepthOnly(target),
                        CorePasses.DepthNormals(target),
                        CorePasses.MotionVectors(target),
                    },
                };

                return result;
            }
        }

        #endregion

        #region Pass

        private static class Passes
        {
            public static PassDescriptor Forward(ToonTarget target)
            {
                ref readonly ToonPasses.Pass pass = ref ToonPasses.ForwardUnlit;
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
                    validVertexBlocks = BlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentColorAlpha,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = RequiredFields.ForwardPass,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    pragmas = CorePragmas.Forward,
                    defines = new DefineCollection(),
                    keywords = Keywords.Default,
                    includes = Includes.Forward,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common,
                };

                CorePasses.AddFogControlToPass(ref result, target);
                CorePasses.AddCustomFogControlToPass(ref result, target);

                return result;
            }

            
        }

        #endregion
        
        #region BlockMasks
        private static class BlockMasks
        {
            public static readonly BlockFieldDescriptor[] Vertex =
            {
                ToonBlockFields.VertexDescription.Position,
                ToonBlockFields.VertexDescription.Normal,
            };
        }
        
        #endregion
        
        #region RequiredFields

        private static class RequiredFields
        {
            public static readonly FieldCollection ForwardPass = new()
            {
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Attributes.uv0,
                ToonStructFields.Varyings.fogFactorAndVertexLight,
            };
        }

        #endregion
        
        #region Keywords

        private static class Keywords
        {
            private static readonly KeywordDescriptor Noise = new()
            {
                displayName = "Noise",
                referenceName = ToonInvertedHullOutline.ShaderKeywords.NoiseKeywordName,
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Local,
                stages = KeywordShaderStage.Vertex,
            };

            private static readonly KeywordDescriptor DistanceFade = new()
            {
                displayName = "Distance Fade",
                referenceName = ToonInvertedHullOutline.ShaderKeywords.DistanceFadeKeywordName,
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Local,
                stages = KeywordShaderStage.Vertex,
            };

            private static readonly KeywordDescriptor NormalSemantic = new()
            {
                displayName = "Normal Semantic",
                referenceName = "",
                type = KeywordType.Enum,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Local,
                stages = KeywordShaderStage.Vertex,
                entries = new KeywordEntry[]
                {
                    new() { displayName = "Off", referenceName = "" },
                    new()
                    {
                        displayName = "UV2",
                        referenceName = ToonInvertedHullOutline.ShaderKeywords.NormalSemanticUV2KeywordName[1..],
                    },
                    new()
                    {
                        displayName = "Tangent",
                        referenceName = ToonInvertedHullOutline.ShaderKeywords.NormalSemanticTangentKeywordName[1..],
                    },
                },
            };

            private static readonly KeywordDescriptor FixedScreenSpaceThickness = new()
            {
                displayName = "Fixed Screen-Space Thickness",
                referenceName = ToonInvertedHullOutline.ShaderKeywords.FixedScreenSpaceThicknessKeywordName,
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Local,
                stages = KeywordShaderStage.Vertex,
            };

            public static readonly KeywordCollection Default = new()
            {
                // multi_compile
                Noise,
                DistanceFade,
                NormalSemantic,
                FixedScreenSpaceThickness,
            };
        }

        #endregion

        #region Includes

        private static class Includes
        {
            private const string PreGraph =
                "Packages/com.deltation.toon-rp/Editor/ShaderGraph/Includes/Extensions/InvertedHullOutlinePreGraph.hlsl";
            private const string ForwardPass =
                "Packages/com.deltation.toon-rp/Editor/ShaderGraph/Includes/Extensions/InvertedHullOutlineForwardPass.hlsl";

            public static readonly IncludeCollection Forward = new()
            {
                // Pre-graph
                CoreIncludes.CorePregraph,
                CoreIncludes.ShaderGraphPregraph,
                {PreGraph, IncludeLocation.Pregraph},

                // Post-graph
                CoreIncludes.CorePostgraph,
                { ForwardPass, IncludeLocation.Postgraph },
            };
        }

        #endregion
    }
}