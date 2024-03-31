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
            target.ControlStencil = false;

            // Process SubShaders
            context.AddSubShader(
                PostProcessSubShader(SubShaders.InvertedHullOutline(target, target.RenderType, target.RenderQueueString)
                )
            );
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            base.GetActiveBlocks(ref context);

            context.AddBlock(ToonBlockFields.VertexDescription.OutlineThickness);
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
                        Passes.DepthOnly(target),
                        Passes.DepthNormals(target),
                        Passes.MotionVectors(target),
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
                    displayName = "Toon RP Outline (Inverted Hull)",
                    referenceName = pass.ReferenceName,
                    useInPreview = true,

                    // Template
                    passTemplatePath = ToonTarget.UberTemplatePath,
                    sharedTemplateDirectories = ToonTarget.SharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = BlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentColor,

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

            public static PassDescriptor DepthOnly(ToonTarget target)
            {
                ref readonly ToonPasses.Pass pass = ref ToonPasses.DepthOnly;
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "Toon RP Outline (Inverted Hull) Depth Only",
                    referenceName = pass.ReferenceName,
                    lightMode = pass.LightMode,
                    useInPreview = false,

                    // Template
                    passTemplatePath = ToonTarget.UberTemplatePath,
                    sharedTemplateDirectories = ToonTarget.SharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = BlockMasks.Vertex,
                    validPixelBlocks = Array.Empty<BlockFieldDescriptor>(),

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = RequiredFields.DepthOnly,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.DepthOnly(target),
                    pragmas = CorePragmas.Instanced,
                    defines = new DefineCollection(),
                    keywords = Keywords.Default,
                    includes = Includes.DepthOnly,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common,
                };

                return result;
            }

            public static PassDescriptor DepthNormals(ToonTarget target)
            {
                ref readonly ToonPasses.Pass pass = ref ToonPasses.DepthNormals;
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "Toon RP Outline (Inverted Hull) Depth Normals",
                    referenceName = pass.ReferenceName,
                    lightMode = pass.LightMode,
                    useInPreview = false,

                    // Template
                    passTemplatePath = ToonTarget.UberTemplatePath,
                    sharedTemplateDirectories = ToonTarget.SharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = BlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentDepthNormalsNoAlpha,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = RequiredFields.DepthNormals,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.DepthNormals(target),
                    pragmas = CorePragmas.Instanced,
                    defines = new DefineCollection(),
                    keywords = Keywords.Default,
                    includes = Includes.DepthNormals,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common,
                };

                return result;
            }

            public static PassDescriptor MotionVectors(ToonTarget target)
            {
                ref readonly ToonPasses.Pass pass = ref ToonPasses.MotionVectors;
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "Toon RP Outline (Inverted Hull) Motion Vectors",
                    referenceName = pass.ReferenceName,
                    lightMode = pass.LightMode,
                    useInPreview = false,

                    // Template
                    passTemplatePath = ToonTarget.UberTemplatePath,
                    sharedTemplateDirectories = ToonTarget.SharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = BlockMasks.Vertex,
                    validPixelBlocks = Array.Empty<BlockFieldDescriptor>(),

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = RequiredFields.MotionVectors,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.MotionVectors(target),
                    pragmas = CorePragmas.Instanced,
                    defines = new DefineCollection(),
                    keywords = Keywords.Default,
                    includes = Includes.MotionVector,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common,
                };

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
                ToonBlockFields.VertexDescription.OutlineThickness,
                ToonBlockFields.VertexDescription.DepthBias,
            };
        }

        #endregion

        #region RequiredFields

        private static class RequiredFields
        {
            private static readonly FieldCollection BaseAttributes = new()
            {
                StructFields.Attributes.uv0,
                StructFields.Attributes.normalOS,
                StructFields.Attributes.tangentOS,
                StructFields.Attributes.uv2,
            };

            public static readonly FieldCollection ForwardPass = new()
            {
                BaseAttributes,
                ToonStructFields.Varyings.fogFactorAndVertexLight,
            };

            public static readonly FieldCollection DepthOnly = new()
            {
                BaseAttributes,
            };

            public static readonly FieldCollection DepthNormals = new()
            {
                BaseAttributes,
                StructFields.Varyings.normalWS,
            };

            public static readonly FieldCollection MotionVectors = new()
            {
                BaseAttributes,
                ToonStructFields.Attributes.positionOld,
                ToonStructFields.Varyings.positionCsNoJitter,
                ToonStructFields.Varyings.previousPositionCsNoJitter,
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
            private const string MotionVectorPass =
                "Packages/com.deltation.toon-rp/Editor/ShaderGraph/Includes/Extensions/InvertedHullOutlineMotionVectorsPass.hlsl";

            public static readonly IncludeCollection Forward = new()
            {
                // Pre-graph
                CoreIncludes.CorePregraph,
                CoreIncludes.ShaderGraphPregraph,
                { PreGraph, IncludeLocation.Pregraph },

                // Post-graph
                CoreIncludes.CorePostgraph,
                { ForwardPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection DepthOnly = new()
            {
                // Pre-graph
                CoreIncludes.CorePregraph,
                CoreIncludes.ShaderGraphPregraph,
                { PreGraph, IncludeLocation.Pregraph },

                // Post-graph
                CoreIncludes.CorePostgraph,
                { CoreIncludes.DepthOnlyPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection DepthNormals = new()
            {
                // Pre-graph
                CoreIncludes.CorePregraph,
                CoreIncludes.ShaderGraphPregraph,
                { PreGraph, IncludeLocation.Pregraph },

                // Post-graph
                CoreIncludes.CorePostgraph,
                { CoreIncludes.DepthNormalsPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection MotionVector = new()
            {
                // Pre-graph
                CoreIncludes.CorePregraph,
                CoreIncludes.ShaderGraphPregraph,
                { PreGraph, IncludeLocation.Pregraph },

                // Post-graph
                CoreIncludes.CorePostgraph,
                { MotionVectorPass, IncludeLocation.Postgraph },
            };
        }

        #endregion
    }
}