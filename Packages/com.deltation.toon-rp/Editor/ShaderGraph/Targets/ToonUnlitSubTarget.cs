using System;
using DELTation.ToonRP.Editor.ShaderGUI.ShaderGraph;
using UnityEditor;
using UnityEditor.ShaderGraph;
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
            context.AddSubShader(
                PostProcessSubShader(SubShaders.Unlit(target, target.RenderType, target.RenderQueueString))
            );
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

                CorePasses.AddPrePasses(target, ref result);
                CorePasses.AddShadowCasterPass(target, ref result);
                CorePasses.AddMetaPass(target, ref result);

                return result;
            }
        }

        #endregion

        #region Pass

        private static class UnlitPasses
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