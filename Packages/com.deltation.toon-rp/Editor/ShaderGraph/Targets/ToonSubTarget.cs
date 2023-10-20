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

        // Overloads to do inline PassDescriptor modifications
        // NOTE: param order should match PassDescriptor field order for consistency

        #region PassVariant

        internal static PassDescriptor PassVariant(in PassDescriptor source, PragmaCollection pragmas)
        {
            PassDescriptor result = source;
            result.pragmas = pragmas;
            return result;
        }

        #endregion
    }
}