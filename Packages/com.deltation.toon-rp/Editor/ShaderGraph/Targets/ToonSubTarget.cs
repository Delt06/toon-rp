using System;
using DELTation.ToonRP.Editor.ShaderGUI;
using DELTation.ToonRP.Editor.ShaderGUI.ShaderGraph;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;
using static DELTation.ToonRP.Editor.ToonShaderUtils;
using RenderQueue = UnityEngine.Rendering.RenderQueue;

namespace DELTation.ToonRP.Editor.ShaderGraph.Targets
{
    internal abstract class ToonSubTarget : SubTarget<ToonTarget>, IHasMetadata
    {
        private static readonly GUID SourceCodeGuid = new("224570bab10c13c4f8b7ca798294dee1"); // ToonSubTarget.cs

        protected abstract ShaderID ShaderID { get; }

        public virtual string identifier => GetType().Name;

        public virtual ScriptableObject GetMetadataObject(GraphDataReadOnly graphData)
        {
            ToonMetadata metadata = ScriptableObject.CreateInstance<ToonMetadata>();
            metadata.ShaderID = ShaderID;
            metadata.AllowMaterialOverride = target.AllowMaterialOverride;
            metadata.AlphaMode = target.AlphaMode;
            metadata.CastShadows = target.CastShadows;
            return metadata;
        }

        public override void GetFields(ref TargetFieldContext context) { }

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(SourceCodeGuid, AssetCollection.Flags.SourceDependency);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(ToonBlockFields.SurfaceDescription.Alpha,
                target.SurfaceType == SurfaceType.Transparent || target.AlphaClip || target.AllowMaterialOverride
            );
            context.AddBlock(ToonBlockFields.SurfaceDescription.AlphaClipThreshold,
                target.AlphaClip || target.AllowMaterialOverride
            );

            context.AddBlock(ToonBlockFields.SurfaceDescription.CustomFogFactor, target.CustomFog);
            context.AddBlock(ToonBlockFields.SurfaceDescription.CustomFogColor, target.CustomFog);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange,
            Action<string> registerUndo)
        {
            target.AddDefaultMaterialOverrideGUI(ref context, onChange, registerUndo);
            target.AddDefaultSurfacePropertiesGUI(ref context, onChange, registerUndo, false);
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            base.ProcessPreviewMaterial(material);

            material.SetFloat(PropertyNames.RenderQueue, (float) target.RenderQueue);
            material.SetFloat(PropertyNames.QueueControl, (float) QueueControl.Auto);
            material.SetFloat(PropertyNames.QueueOffset, 0.0f);

            if (target.AllowMaterialOverride)
            {
                // copy our target's default settings into the material
                // (technically not necessary since we are always recreating the material from the shader each time,
                // which will pull over the defaults from the shader definition)
                // but if that ever changes, this will ensure the defaults are set
                material.SetFloat(PropertyNames.SurfaceType, (float) target.SurfaceType);
                material.SetFloat(PropertyNames.BlendMode, (float) target.AlphaMode);
                material.SetFloat(PropertyNames.AlphaClipping, target.AlphaClip ? 1.0f : 0.0f);
                material.SetFloat(PropertyNames.AlphaToCoverage, target.AlphaToCoverage ? 1.0f : 0.0f);
                material.SetFloat(PropertyNames.ForceDisableFog, !target.Fog ? 1.0f : 0.0f);
                material.SetFloat(PropertyNames.CustomFog, target.CustomFog ? 1.0f : 0.0f);
                material.SetFloat(PropertyNames.RenderFace, (int) target.RenderFace);
                material.SetFloat(PropertyNames.CastShadows, target.CastShadows ? 1.0f : 0.0f);
                material.SetFloat(PropertyNames.ZWriteControl, (float) target.ZWriteControl);
                material.SetFloat(PropertyNames.ZTest, (float) target.ZTestMode);
            }

            material.SetFloat(PropertyNames.ControlStencil,
                target.ControlStencilEffectivelyEnabled ? 1.0f : 0.0f
            );

            if (target.ControlStencilEffectivelyEnabled || target.AllowMaterialOverride)
            {
                material.SetFloat(PropertyNames.StencilPreset, (float) StencilPreset.None);
                material.SetFloat(PropertyNames.ForwardStencilRef, 0);
                material.SetFloat(PropertyNames.ForwardStencilReadMask, 255);
                material.SetFloat(PropertyNames.ForwardStencilWriteMask, 255);
                material.SetFloat(PropertyNames.ForwardStencilComp, (float) CompareFunction.Disabled);
                material.SetFloat(PropertyNames.ForwardStencilPass, (float) StencilOp.Keep);
            }
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);

            collector.AddEnumProperty(PropertyNames.RenderQueue, (float) target.RenderQueue, typeof(RenderQueue),
                "Render Queue"
            );
            collector.AddEnumProperty(PropertyNames.QueueControl, (float) QueueControl.Auto, typeof(QueueControl),
                "Queue Control"
            );
            collector.AddFloatProperty(PropertyNames.QueueOffset, 0.0f);

            if (target.AllowMaterialOverride)
            {
                collector.AddFloatProperty(PropertyNames.CastShadows, target.CastShadows ? 1.0f : 0.0f);

                // setup properties using the defaults
                collector.AddFloatProperty(PropertyNames.SurfaceType, (float) target.SurfaceType);
                collector.AddFloatProperty(PropertyNames.BlendMode, (float) target.AlphaMode);
                collector.AddFloatProperty(PropertyNames.AlphaClipping, target.AlphaClip ? 1.0f : 0.0f);
                collector.AddFloatProperty(PropertyNames.AlphaToCoverage, target.AlphaToCoverage ? 1.0f : 0.0f);
                collector.AddFloatProperty(PropertyNames.ForceDisableFog, !target.Fog ? 1.0f : 0.0f);
                collector.AddFloatProperty(PropertyNames.CustomFog, target.CustomFog ? 1.0f : 0.0f);
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

            collector.AddFloatProperty(PropertyNames.ControlStencil,
                target.ControlStencilEffectivelyEnabled ? 1.0f : 0.0f
            );

            if (target.ControlStencilEffectivelyEnabled || target.AllowMaterialOverride)
            {
                collector.AddEnumProperty(PropertyNames.StencilPreset, 0.0f, typeof(StencilPreset),
                    "Stencil Preset"
                );
                collector.AddIntProperty(PropertyNames.ForwardStencilRef, 0, "Ref");
                collector.AddIntProperty(PropertyNames.ForwardStencilReadMask, 255, "Read Mask");
                collector.AddIntProperty(PropertyNames.ForwardStencilWriteMask, 255, "Write Mask");
                collector.AddEnumProperty(PropertyNames.ForwardStencilComp, (float) CompareFunction.Disabled,
                    typeof(CompareFunction), "Comp"
                );
                collector.AddEnumProperty(PropertyNames.ForwardStencilPass, (float) StencilOp.Keep, typeof(StencilOp),
                    "Pass"
                );
            }
        }

        protected static SubShaderDescriptor PostProcessSubShader(SubShaderDescriptor subShaderDescriptor) =>
            subShaderDescriptor;
    }

    internal static class SubShaderUtils
    {
        internal static void AddFloatProperty(this PropertyCollector collector, string referenceName,
            float defaultValue, HLSLDeclaration declarationType = HLSLDeclaration.DoNotDeclare)
        {
            collector.AddShaderProperty(new Vector1ShaderProperty
                {
                    floatType = FloatType.Default,
                    hidden = true,
                    overrideHLSLDeclaration = true,
                    hlslDeclarationOverride = declarationType,
                    value = defaultValue,
                    displayName = referenceName,
                    overrideReferenceName = referenceName,
                }
            );
        }

        internal static void AddIntProperty(this PropertyCollector collector, string referenceName,
            float defaultValue, string displayName = null,
            HLSLDeclaration declarationType = HLSLDeclaration.DoNotDeclare)
        {
            collector.AddShaderProperty(new Vector1ShaderProperty
                {
                    floatType = FloatType.Integer,
                    hidden = true,
                    overrideHLSLDeclaration = true,
                    hlslDeclarationOverride = declarationType,
                    value = defaultValue,
                    displayName = displayName ?? referenceName,
                    overrideReferenceName = referenceName,
                }
            );
        }

        internal static void AddEnumProperty(this PropertyCollector collector, string referenceName,
            float defaultValue, Type enumType, string displayName = null,
            HLSLDeclaration declarationType = HLSLDeclaration.DoNotDeclare)
        {
            collector.AddShaderProperty(new Vector1ShaderProperty
                {
                    floatType = FloatType.Enum,
                    hidden = true,
                    overrideHLSLDeclaration = true,
                    hlslDeclarationOverride = declarationType,
                    value = defaultValue,
                    displayName = displayName ?? referenceName,
                    overrideReferenceName = referenceName,
                    enumType = EnumType.CSharpEnum,
                    cSharpEnumType = enumType,
                }
            );
        }

        internal static void AddToggleProperty(this PropertyCollector collector, string referenceName,
            bool defaultValue, HLSLDeclaration declarationType = HLSLDeclaration.DoNotDeclare)
        {
            collector.AddShaderProperty(new BooleanShaderProperty
                {
                    value = defaultValue,
                    hidden = true,
                    overrideHLSLDeclaration = true,
                    hlslDeclarationOverride = declarationType,
                    displayName = referenceName,
                    overrideReferenceName = referenceName,
                }
            );
        }
    }
}