using System;
using System.Linq;
using DELTation.ToonRP.Editor.ShaderGUI;
using DELTation.ToonRP.Editor.ShaderGUI.ShaderGraph;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static DELTation.ToonRP.Editor.ToonShaderUtils;

namespace DELTation.ToonRP.Editor.ShaderGraph.Targets
{
    internal sealed class ToonDefaultSubTarget : ToonSubTarget
    {
        private static readonly GUID
            SourceCodeGuid = new("7fbd1157198711d4cb1f56c8e4dd1cf9"); // ToonDefaultSubTarget.cs

        public ToonDefaultSubTarget() => displayName = "Default";

        protected override ShaderID ShaderID => ShaderID.SgDefault;

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(SourceCodeGuid, AssetCollection.Flags.SourceDependency);
            base.Setup(ref context);

            Type toonRpType = typeof(ToonRenderPipelineAsset);
            if (!context.HasCustomEditorForRenderPipeline(toonRpType))
            {
                Type gui = typeof(ToonRpShaderGraphDefaultShaderGui);
                context.AddCustomEditorForRenderPipeline(gui.FullName, toonRpType);
            }

            // Process SubShaders
            context.AddSubShader(
                PostProcessSubShader(SubShaders.DefaultSubShader(target, this, target.RenderType, target.RenderQueue))
            );
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            if (target.AllowMaterialOverride)
            {
                // copy our target's default settings into the material
                // (technically not necessary since we are always recreating the material from the shader each time,
                // which will pull over the defaults from the shader definition)
                // but if that ever changes, this will ensure the defaults are set
                material.SetFloat(PropertyNames.CastShadows, target.CastShadows ? 1.0f : 0.0f);
                material.SetFloat(PropertyNames.ReceiveShadows, target.ReceiveShadows ? 1.0f : 0.0f);
                material.SetFloat(PropertyNames.SurfaceType, (float) target.SurfaceType);
                material.SetFloat(PropertyNames.BlendMode, (float) target.AlphaMode);
                material.SetFloat(PropertyNames.AlphaClipping, target.AlphaClip ? 1.0f : 0.0f);
                material.SetFloat(PropertyNames.Specular, Specular ? 1.0f : 0.0f);
                material.SetFloat(PropertyNames.ForceDisableFogPropertyName, !target.Fog ? 1.0f : 0.0f);
                material.SetFloat(PropertyNames.ForceDisableEnvironmentLightPropertyName,
                    !EnvironmentLighting ? 1.0f : 0.0f
                );
                material.SetFloat(PropertyNames.RenderFace, (int) target.RenderFace);
                material.SetFloat(PropertyNames.ZWriteControl, (float) target.ZWriteControl);
                material.SetFloat(PropertyNames.ZTest, (float) target.ZTestMode);
            }

            material.SetFloat(PropertyNames.ReceiveBlobShadows, 0.0f);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            BlockFieldDescriptor[] descs = context.blocks.Select(x => x.descriptor).ToArray();

            // Default -- always controlled by subtarget
            context.AddField(ToonFields.NormalDropOffOS, NormalDropOffSpace == NormalDropOffSpace.Object);
            context.AddField(ToonFields.NormalDropOffTs, NormalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddField(ToonFields.NormalDropOffWs, NormalDropOffSpace == NormalDropOffSpace.World);
            context.AddField(ToonFields.Normal, descs.Contains(BlockFields.SurfaceDescription.NormalOS) ||
                                                descs.Contains(BlockFields.SurfaceDescription.NormalTS) ||
                                                descs.Contains(BlockFields.SurfaceDescription.NormalWS)
            );
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(ToonBlockFields.SurfaceDescription.NormalOs,
                NormalDropOffSpace == NormalDropOffSpace.Object
            );
            context.AddBlock(ToonBlockFields.SurfaceDescription.NormalTs,
                NormalDropOffSpace == NormalDropOffSpace.Tangent
            );
            context.AddBlock(ToonBlockFields.SurfaceDescription.NormalWs, NormalDropOffSpace == NormalDropOffSpace.World
            );

            context.AddBlock(ToonBlockFields.SurfaceDescription.Alpha,
                target.SurfaceType == SurfaceType.Transparent || target.AlphaClip || target.AllowMaterialOverride
            );
            context.AddBlock(ToonBlockFields.SurfaceDescription.AlphaClipThreshold,
                target.AlphaClip || target.AllowMaterialOverride
            );
            
            context.AddBlock(ToonBlockFields.SurfaceDescription.SpecularColor,
                Specular
            );
            
            context.AddBlock(ToonBlockFields.SurfaceDescription.SpecularSizeOffset,
                Specular
            );

            context.AddBlock(ToonBlockFields.SurfaceDescription.GlobalRampUV);
            context.AddBlock(ToonBlockFields.SurfaceDescription.ShadowColor);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            if (target.AllowMaterialOverride)
            {
                collector.AddFloatProperty(PropertyNames.CastShadows, target.CastShadows ? 1.0f : 0.0f);
                collector.AddFloatProperty(PropertyNames.ReceiveShadows, target.ReceiveShadows ? 1.0f : 0.0f);

                // setup properties using the defaults
                collector.AddFloatProperty(PropertyNames.SurfaceType, (float) target.SurfaceType);
                collector.AddFloatProperty(PropertyNames.BlendMode, (float) target.AlphaMode);
                collector.AddFloatProperty(PropertyNames.AlphaClipping, target.AlphaClip ? 1.0f : 0.0f);
                collector.AddFloatProperty(PropertyNames.Specular, Specular ? 1.0f : 0.0f);
                collector.AddFloatProperty(PropertyNames.ForceDisableFogPropertyName, !target.Fog ? 1.0f : 0.0f);
                collector.AddFloatProperty(PropertyNames.ForceDisableEnvironmentLightPropertyName,
                    !EnvironmentLighting ? 1.0f : 0.0f
                );
                collector.AddFloatProperty(PropertyNames.BlendSrc, 1.0f
                ); // always set by material inspector, ok to have incorrect values here
                collector.AddFloatProperty(PropertyNames.BlendDst, 0.0f
                ); // always set by material inspector, ok to have incorrect values here
                collector.AddToggleProperty(PropertyNames.ZWrite, target.SurfaceType == SurfaceType.Opaque);
                collector.AddFloatProperty(PropertyNames.ZWriteControl, (float) target.ZWriteControl);
                collector.AddFloatProperty(PropertyNames.ZTest, (float) target.ZTestMode
                ); // ztest mode is designed to directly pass as ztest
                collector.AddFloatProperty(PropertyNames.RenderFace, (float) target.RenderFace
                ); // render face enum is designed to directly pass as a cull mode
            }

            collector.AddFloatProperty(PropertyNames.ReceiveBlobShadows, 0.0f);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange,
            Action<string> registerUndo)
        {
            target.AddDefaultMaterialOverrideGUI(ref context, onChange, registerUndo);

            target.AddDefaultSurfacePropertiesGUI(ref context, onChange, registerUndo, true);
            
            context.AddProperty("Specular", new Toggle { value = Specular }, evt =>
                {
                    if (Equals(Specular, evt.newValue))
                    {
                        return;
                    }

                    registerUndo("Change Specular");
                    Specular = evt.newValue;
                    onChange();
                }
            );

            context.AddProperty("Environment Lighting", new Toggle { value = EnvironmentLighting }, evt =>
                {
                    if (Equals(EnvironmentLighting, evt.newValue))
                    {
                        return;
                    }

                    registerUndo("Change Environment Lighting");
                    EnvironmentLighting = evt.newValue;
                    onChange();
                }
            );

            context.AddProperty("Fragment Normal Space",
                new EnumField(NormalDropOffSpace.Tangent) { value = NormalDropOffSpace }, evt =>
                {
                    if (Equals(NormalDropOffSpace, evt.newValue))
                    {
                        return;
                    }

                    registerUndo("Change Fragment Normal Space");
                    NormalDropOffSpace = (NormalDropOffSpace) evt.newValue;
                    onChange();
                }
            );
        }

        // TODO: check if we need this
        // protected override int ComputeMaterialNeedsUpdateHash()
        // {
        //     int hash = base.ComputeMaterialNeedsUpdateHash();
        //     hash = hash * 23 + target.allowMaterialOverride.GetHashCode();
        //     return hash;
        // }

        #region SubShader

        private static class SubShaders
        {
            public static SubShaderDescriptor DefaultSubShader(ToonTarget target, ToonDefaultSubTarget subTarget,
                string renderType, string renderQueue)
            {
                var result = new SubShaderDescriptor
                {
                    pipelineTag = ToonRenderPipeline.PipelineTag,
                    renderType = renderType,
                    renderQueue = renderQueue,
                    generatesPreview = true,
                    passes = new PassCollection
                    {
                        DefaultPasses.Forward(target, subTarget),
                    },
                };

                // cull the shadowcaster pass if we know it will never be used
                if (target.CastShadows || target.AllowMaterialOverride)
                {
                    result.passes.Add(CorePasses.ShadowCaster(target));
                }

                if (target.MayWriteDepth)
                {
                    result.passes.Add(CorePasses.DepthOnly(target));
                    result.passes.Add(CorePasses.DepthNormals(target));
                    result.passes.Add(CorePasses.MotionVectors(target));
                }

                return result;
            }
        }

        #endregion

        #region Passes

        private static class DefaultPasses
        {
            private static void AddReceiveShadowsControlToPass(ref PassDescriptor pass, ToonTarget target,
                bool receiveShadows)
            {
                if (target.AllowMaterialOverride)
                {
                    pass.keywords.Add(DefaultKeywords.ReceiveShadowsOff);
                }
                else if (!receiveShadows)
                {
                    pass.defines.Add(DefaultKeywords.ReceiveShadowsOff, 1);
                }
            }

            private static void AddEnvironmentLightingControlToPass(ref PassDescriptor pass, ToonTarget target,
                bool environmentLighting)
            {
                if (target.AllowMaterialOverride)
                {
                    pass.keywords.Add(CoreKeywordDescriptors.ForceDisableEnvironmentLight);
                }
                else if (!environmentLighting)
                {
                    pass.defines.Add(CoreKeywordDescriptors.ForceDisableEnvironmentLight, 1);
                }
            }
            
            private static void AddSpecularControlToPass(ref PassDescriptor pass, ToonTarget target,
                bool specular)
            {
                if (target.AllowMaterialOverride)
                {
                    pass.keywords.Add(CoreKeywordDescriptors.Specular);
                }
                else if (specular)
                {
                    pass.defines.Add(CoreKeywordDescriptors.Specular, 1);
                }
            }

            public static PassDescriptor Forward(ToonTarget target, ToonDefaultSubTarget subTarget,
                PragmaCollection pragmas = null)
            {
                ref readonly ToonPasses.Pass pass = ref ToonPasses.Forward;
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = pass.Name,
                    referenceName = pass.ReferenceName,
                    lightMode = pass.LightMode,
                    useInPreview = true,

                    // Template
                    passTemplatePath = ToonTarget.UberTemplatePath,
                    sharedTemplateDirectories = ToonTarget.SharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = DefaultBlockMasks.VertexDefault,
                    validPixelBlocks = DefaultBlockMasks.FragmentDefault,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = DefaultRequiredFields.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target),
                    pragmas = pragmas ?? CorePragmas.Forward, // NOTE: SM 2.0 only GL
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection { DefaultKeywords.Forward },
                    includes = DefaultIncludes.Forward,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common,
                };

                CorePasses.AddTargetSurfaceControlsToPass(ref result, target);
                AddReceiveShadowsControlToPass(ref result, target, target.ReceiveShadows);
                AddEnvironmentLightingControlToPass(ref result, target, subTarget.EnvironmentLighting);
                AddSpecularControlToPass(ref result, target, subTarget.Specular);

                return result;
            }
        }

        #endregion

        #region PortMasks

        private static class DefaultBlockMasks
        {
            public static readonly BlockFieldDescriptor[] VertexDefault =
            {
                ToonBlockFields.VertexDescription.Position,
                ToonBlockFields.VertexDescription.Normal,
                ToonBlockFields.VertexDescription.Tangent,
            };

            public static readonly BlockFieldDescriptor[] FragmentDefault =
            {
                ToonBlockFields.SurfaceDescription.Albedo,
                ToonBlockFields.SurfaceDescription.Alpha,
                ToonBlockFields.SurfaceDescription.AlphaClipThreshold,
                ToonBlockFields.SurfaceDescription.Emission,
                ToonBlockFields.SurfaceDescription.GlobalRampUV,
                ToonBlockFields.SurfaceDescription.ShadowColor,
                ToonBlockFields.SurfaceDescription.SpecularColor,
                ToonBlockFields.SurfaceDescription.SpecularSizeOffset,
            };
        }

        #endregion

        #region RequiredFields

        private static class DefaultRequiredFields
        {
            public static readonly FieldCollection Forward = new()
            {
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,
                StructFields.Varyings.viewDirectionWS,
                ToonStructFields.Varyings.fogFactorAndVertexLight,
            };
        }

        #endregion

        #region Keywords

        private static class DefaultKeywords
        {
            // TODO: implement or remove this
            public static readonly KeywordDescriptor ReceiveShadowsOff = new()
            {
                displayName = "Receive Shadows Off",
                referenceName = "Receive Shadows Off",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static readonly KeywordCollection Forward = new()
            {
                // multi_compile
                CoreKeywordDescriptors.ToonRpGlobalRamp,

                CoreKeywordDescriptors.ToonRpDirectionalShadows,
                CoreKeywordDescriptors.ToonRpShadowSmoothingMode,
                CoreKeywordDescriptors.ToonRpPoissonSamplingMode,
                CoreKeywordDescriptors.ToonRpPoissonSamplingEarlyBail,
                CoreKeywordDescriptors.ToonRpShadowsRampCrisp,
                CoreKeywordDescriptors.ToonRpShadowsPattern,

                CoreKeywordDescriptors.ToonRpAdditionalLights,
                CoreKeywordDescriptors.ToonRpTiledLighting,

                CoreKeywordDescriptors.ToonRpSsao,

                // shader_feature
                CoreKeywordDescriptors.ReceiveBlobShadows,
            };
        }

        #endregion

        #region Includes

        private static class DefaultIncludes
        {
            private const string ForwardPass =
                "Packages/com.deltation.toon-rp/Editor/ShaderGraph/Includes/DefaultPass.hlsl";
            private const string ToonLighting =
                "Packages/com.deltation.toon-rp/ShaderLibrary/ToonLighting.hlsl";

            public static readonly IncludeCollection Forward = new()
            {
                // Pre-graph
                CoreIncludes.CorePregraph,
                CoreIncludes.ShaderGraphPregraph,
                { ToonLighting, IncludeLocation.Pregraph },

                // Post-graph
                CoreIncludes.CorePostgraph,
                { ForwardPass, IncludeLocation.Postgraph },
            };
        }

        #endregion

        // ReSharper disable Unity.RedundantSerializeFieldAttribute
        [field: SerializeField]
        private bool Specular { get; set; } = true;
        
        [field: SerializeField]
        private bool EnvironmentLighting { get; set; } = true;

        [field: SerializeField]
        private NormalDropOffSpace NormalDropOffSpace { get; set; } = NormalDropOffSpace.Tangent;
        // ReSharper restore Unity.RedundantSerializeFieldAttribute
    }
}