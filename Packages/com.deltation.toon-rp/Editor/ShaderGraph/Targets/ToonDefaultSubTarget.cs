using System;
using System.Linq;
using DELTation.ToonRP.Editor.ShaderGUI;
using DELTation.ToonRP.Editor.ShaderGUI.ShaderGraph;
using UnityEditor;
using UnityEditor.ShaderGraph;
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
                PostProcessSubShader(SubShaders.DefaultSubShader(target, this, target.RenderType,
                        target.RenderQueueString
                    )
                )
            );
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            base.ProcessPreviewMaterial(material);

            if (target.AllowMaterialOverride)
            {
                material.SetFloat(PropertyNames.ReceiveShadows, target.ReceiveShadows ? 1.0f : 0.0f);
                material.SetFloat(PropertyNames.OverrideRamp, OverrideRamp ? 1.0f : 0.0f);
                material.SetFloat(PropertyNames.Specular, Specular ? 1.0f : 0.0f);
                material.SetFloat(PropertyNames.AdditionalLightsSpecular, AdditionalLightsSpecular ? 1.0f : 0.0f);
                material.SetFloat(PropertyNames.Rim, Rim ? 1.0f : 0.0f);
                material.SetFloat(PropertyNames.ForceDisableEnvironmentLight,
                    !EnvironmentLighting ? 1.0f : 0.0f
                );
            }

            material.SetFloat(PropertyNames.ReceiveBlobShadows, 0.0f);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            BlockFieldDescriptor[] descs = context.blocks.Select(x => x.descriptor).ToArray();

            // Default -- always controlled by subtarget
            context.AddField(ToonFields.PositionDropOffWs);
            context.AddField(ToonFields.NormalDropOffOS, NormalDropOffSpace == NormalDropOffSpace.Object);
            context.AddField(ToonFields.NormalDropOffTs, NormalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddField(ToonFields.NormalDropOffWs, NormalDropOffSpace == NormalDropOffSpace.World);
            context.AddField(ToonFields.Normal, descs.Contains(ToonBlockFields.SurfaceDescription.NormalOs) ||
                                                descs.Contains(ToonBlockFields.SurfaceDescription.NormalTs) ||
                                                descs.Contains(ToonBlockFields.SurfaceDescription.NormalWs)
            );
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            base.GetActiveBlocks(ref context);

            context.AddBlock(ToonBlockFields.SurfaceDescription.EmissionShadowBlend);

            context.AddBlock(ToonBlockFields.SurfaceDescription.PositionWs);
            context.AddBlock(ToonBlockFields.SurfaceDescription.NormalOs,
                NormalDropOffSpace == NormalDropOffSpace.Object
            );
            context.AddBlock(ToonBlockFields.SurfaceDescription.NormalTs,
                NormalDropOffSpace == NormalDropOffSpace.Tangent
            );
            context.AddBlock(ToonBlockFields.SurfaceDescription.NormalWs, NormalDropOffSpace == NormalDropOffSpace.World
            );

            context.AddBlock(ToonBlockFields.SurfaceDescription.OverrideRampThreshold, OverrideRamp);
            context.AddBlock(ToonBlockFields.SurfaceDescription.OverrideRampSmoothness, OverrideRamp);

            context.AddBlock(ToonBlockFields.SurfaceDescription.SpecularColor, Specular);
            context.AddBlock(ToonBlockFields.SurfaceDescription.SpecularSizeOffset, Specular);
            context.AddBlock(ToonBlockFields.SurfaceDescription.OverrideRampSpecularThreshold, OverrideRamp && Specular
            );
            context.AddBlock(ToonBlockFields.SurfaceDescription.OverrideRampSpecularSmoothness, OverrideRamp && Specular
            );

            context.AddBlock(ToonBlockFields.SurfaceDescription.RimColor, Rim);
            context.AddBlock(ToonBlockFields.SurfaceDescription.RimSizeOffset, Rim);
            context.AddBlock(ToonBlockFields.SurfaceDescription.OverrideRampRimThreshold, OverrideRamp && Rim);
            context.AddBlock(ToonBlockFields.SurfaceDescription.OverrideRampRimSmoothness, OverrideRamp && Rim);

            context.AddBlock(ToonBlockFields.SurfaceDescription.GlobalRampUV, !OverrideRamp);
            context.AddBlock(ToonBlockFields.SurfaceDescription.ShadowColor);
            context.AddBlock(ToonBlockFields.SurfaceDescription.DiffuseOffset);
            context.AddBlock(ToonBlockFields.SurfaceDescription.MainLightOcclusion);
            context.AddBlock(ToonBlockFields.SurfaceDescription.ShadowReceivePositionOffset);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);

            if (target.AllowMaterialOverride)
            {
                collector.AddFloatProperty(PropertyNames.ReceiveShadows, target.ReceiveShadows ? 1.0f : 0.0f);

                collector.AddFloatProperty(PropertyNames.OverrideRamp, OverrideRamp ? 1.0f : 0.0f);
                collector.AddFloatProperty(PropertyNames.Specular, Specular ? 1.0f : 0.0f);
                collector.AddFloatProperty(PropertyNames.AdditionalLightsSpecular,
                    AdditionalLightsSpecular ? 1.0f : 0.0f
                );
                collector.AddFloatProperty(PropertyNames.Rim, Rim ? 1.0f : 0.0f);
                collector.AddFloatProperty(PropertyNames.ForceDisableEnvironmentLight,
                    !EnvironmentLighting ? 1.0f : 0.0f
                );
            }

            collector.AddFloatProperty(PropertyNames.ReceiveBlobShadows, 0.0f);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange,
            Action<string> registerUndo)
        {
            target.AddDefaultMaterialOverrideGUI(ref context, onChange, registerUndo);
            target.AddDefaultSurfacePropertiesGUI(ref context, onChange, registerUndo, true);

            context.AddProperty("Override Ramp", new Toggle { value = OverrideRamp }, evt =>
                {
                    if (Equals(OverrideRamp, evt.newValue))
                    {
                        return;
                    }

                    registerUndo("Change Override Ramp");
                    OverrideRamp = evt.newValue;
                    onChange();
                }
            );

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

            if (Specular)
            {
                context.AddProperty("Additional Lights Specular", new Toggle { value = AdditionalLightsSpecular },
                    evt =>
                    {
                        if (Equals(AdditionalLightsSpecular, evt.newValue))
                        {
                            return;
                        }

                        registerUndo("Change Additional Lights Specular");
                        AdditionalLightsSpecular = evt.newValue;
                        onChange();
                    }
                );
            }

            context.AddProperty("Rim", new Toggle { value = Rim }, evt =>
                {
                    if (Equals(Rim, evt.newValue))
                    {
                        return;
                    }

                    registerUndo("Change Rim");
                    Rim = evt.newValue;
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

                CorePasses.AddPrePasses(target, ref result);
                CorePasses.AddShadowCasterPass(target, ref result);
                CorePasses.AddMetaPass(target, ref result);

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

            private static void AddOverrideRampControlToPass(ref PassDescriptor pass, ToonTarget target,
                bool overrideRamp)
            {
                if (target.AllowMaterialOverride)
                {
                    pass.keywords.Add(CoreKeywordDescriptors.OverrideRamp);
                }
                else if (overrideRamp)
                {
                    pass.defines.Add(CoreKeywordDescriptors.OverrideRamp, 1);
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

            private static void AddAdditionalLightsSpecularControlToPass(ref PassDescriptor pass, ToonTarget target,
                bool additionalLightsSpecular)
            {
                if (target.AllowMaterialOverride)
                {
                    pass.keywords.Add(CoreKeywordDescriptors.AdditionalLightsSpecular);
                }
                else if (additionalLightsSpecular)
                {
                    pass.defines.Add(CoreKeywordDescriptors.AdditionalLightsSpecular, 1);
                }
            }


            private static void AddRimControlToPass(ref PassDescriptor pass, ToonTarget target,
                bool rim)
            {
                if (target.AllowMaterialOverride)
                {
                    pass.keywords.Add(CoreKeywordDescriptors.Rim);
                }
                else if (rim)
                {
                    pass.defines.Add(CoreKeywordDescriptors.Rim, 1);
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
                AddOverrideRampControlToPass(ref result, target, subTarget.OverrideRamp);
                AddSpecularControlToPass(ref result, target, subTarget.Specular);
                AddAdditionalLightsSpecularControlToPass(ref result, target, subTarget.AdditionalLightsSpecular);
                AddRimControlToPass(ref result, target, subTarget.Rim);

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
                ToonBlockFields.VertexDescription.DepthBias,
            };

            public static readonly BlockFieldDescriptor[] FragmentDefault =
            {
                ToonBlockFields.SurfaceDescription.PositionWs,
                ToonBlockFields.SurfaceDescription.NormalTs,
                ToonBlockFields.SurfaceDescription.NormalOs,
                ToonBlockFields.SurfaceDescription.NormalWs,

                ToonBlockFields.SurfaceDescription.Albedo,
                ToonBlockFields.SurfaceDescription.Alpha,
                ToonBlockFields.SurfaceDescription.AlphaClipThreshold,
                ToonBlockFields.SurfaceDescription.CustomFogFactor,
                ToonBlockFields.SurfaceDescription.CustomFogColor,
                ToonBlockFields.SurfaceDescription.Emission,
                ToonBlockFields.SurfaceDescription.EmissionShadowBlend,
                ToonBlockFields.SurfaceDescription.GlobalRampUV,
                ToonBlockFields.SurfaceDescription.ShadowColor,
                ToonBlockFields.SurfaceDescription.DiffuseOffset,
                ToonBlockFields.SurfaceDescription.MainLightOcclusion,
                ToonBlockFields.SurfaceDescription.ShadowReceivePositionOffset,

                ToonBlockFields.SurfaceDescription.OverrideRampThreshold,
                ToonBlockFields.SurfaceDescription.OverrideRampSmoothness,

                ToonBlockFields.SurfaceDescription.SpecularColor,
                ToonBlockFields.SurfaceDescription.SpecularSizeOffset,
                ToonBlockFields.SurfaceDescription.OverrideRampSpecularThreshold,
                ToonBlockFields.SurfaceDescription.OverrideRampSpecularSmoothness,

                ToonBlockFields.SurfaceDescription.RimColor,
                ToonBlockFields.SurfaceDescription.RimSizeOffset,
                ToonBlockFields.SurfaceDescription.OverrideRampRimThreshold,
                ToonBlockFields.SurfaceDescription.OverrideRampRimSmoothness,
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
                ToonStructFields.Varyings.fogFactorAndVertexLight,
                ToonStructFields.Varyings.lightmapUv,
            };
        }

        #endregion

        #region Keywords

        private static class DefaultKeywords
        {
            public static readonly KeywordDescriptor ReceiveShadowsOff = new()
            {
                displayName = "Receive Shadows Off",
                referenceName = ShaderKeywords.ReceiveShadowsOff,
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static readonly KeywordCollection Forward = new()
            {
                // multi_compile
                CoreKeywordDescriptors.ToonRpGlobalRamp,

                CoreKeywordDescriptors.ToonRpDirectionalShadows,
                CoreKeywordDescriptors.ToonRpAdditionalShadows,
                CoreKeywordDescriptors.ToonRpShadowSmoothingMode,
                CoreKeywordDescriptors.ToonRpPoissonSamplingMode,
                CoreKeywordDescriptors.ToonRpPoissonSamplingEarlyBail,
                CoreKeywordDescriptors.ToonRpShadowsRampCrisp,
                CoreKeywordDescriptors.ToonRpShadowsPattern,

                CoreKeywordDescriptors.ToonRpAdditionalLights,

                CoreKeywordDescriptors.LightmapShadowMixing,
                CoreKeywordDescriptors.ShadowsShadowmask,
                CoreKeywordDescriptors.DirLightmapCombined,
                CoreKeywordDescriptors.LightmapOn,

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
        private bool OverrideRamp { get; set; }

        [field: SerializeField]
        private bool Specular { get; set; } = true;

        [field: SerializeField]
        private bool AdditionalLightsSpecular { get; set; }

        [field: SerializeField]
        private bool Rim { get; set; } = true;

        [field: SerializeField]
        private bool EnvironmentLighting { get; set; } = true;

        [field: SerializeField]
        private NormalDropOffSpace NormalDropOffSpace { get; set; } = NormalDropOffSpace.Tangent;

        // ReSharper restore Unity.RedundantSerializeFieldAttribute
    }
}