using System;
using DELTation.ToonRP.Editor.ShaderGUI;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using static DELTation.ToonRP.Editor.ToonShaderUtils;

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

        public override void ProcessPreviewMaterial(Material material)
        {
            base.ProcessPreviewMaterial(material);

            if (target.ControlOutlinesStencilLayerEffectivelyEnabled || target.AllowMaterialOverride)
            {
                material.SetFloat(PropertyNames.OutlinesStencilLayer, 0.0f);
                material.SetFloat(PropertyNames.ForwardStencilRef, 0.0f);
                material.SetFloat(PropertyNames.ForwardStencilWriteMask, 0.0f);
                material.SetFloat(PropertyNames.ForwardStencilComp, 0.0f);
                material.SetFloat(PropertyNames.ForwardStencilPass, 0.0f);
            }
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);

            collector.AddFloatProperty(PropertyNames.ControlOutlinesStencilLayer,
                target.ControlOutlinesStencilLayerEffectivelyEnabled ? 1.0f : 0.0f
            );

            if (target.ControlOutlinesStencilLayerEffectivelyEnabled || target.AllowMaterialOverride)
            {
                collector.AddEnumProperty(PropertyNames.OutlinesStencilLayer, 0.0f, typeof(StencilLayer),
                    "Outlines Stencil Layer"
                );
                collector.AddFloatProperty(PropertyNames.ForwardStencilRef, 0.0f);
                collector.AddFloatProperty(PropertyNames.ForwardStencilWriteMask, 0.0f);
                collector.AddFloatProperty(PropertyNames.ForwardStencilComp, 0.0f);
                collector.AddFloatProperty(PropertyNames.ForwardStencilPass, 0.0f);
            }
        }

        protected static SubShaderDescriptor PostProcessSubShader(SubShaderDescriptor subShaderDescriptor) =>
            subShaderDescriptor;

        // TODO: Check if we need this
        // private int lastMaterialNeedsUpdateHash = 0;
        // protected virtual int ComputeMaterialNeedsUpdateHash() => 0;
        // public override object saveContext
        // {
        //     get
        //     {
        //         int hash = ComputeMaterialNeedsUpdateHash();
        //         bool needsUpdate = hash != lastMaterialNeedsUpdateHash;
        //         if (needsUpdate)
        //             lastMaterialNeedsUpdateHash = hash;
        //
        //         return new ToonShaderGraphSaveContext { updateMaterials = needsUpdate };
        //     }
        // }
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