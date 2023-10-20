using System.Diagnostics.CodeAnalysis;
using UnityEditor.ShaderGraph;

namespace DELTation.ToonRP.Editor.ShaderGraph
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static class ToonStructFields
    {
        public struct Attributes
        {
            private const string Name = "Attributes";

            public static readonly FieldDescriptor positionOld = new(Name, "positionOld", "ATTRIBUTES_NEED_TEXCOORD4",
                ShaderValueType.Float3,
                "TEXCOORD4",
                subscriptOptions: StructFieldOptions.Optional
            );
        }

        public struct Varyings
        {
            private const string Name = "Varyings";

            public static readonly FieldDescriptor positionCsNoJitter = new(Name, "positionCsNoJitter", "",
                ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional
            );
            public static readonly FieldDescriptor previousPositionCsNoJitter = new(Name, "previousPositionCsNoJitter",
                "", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional
            );
            public static readonly FieldDescriptor fogFactorAndVertexLight = new(Name, "fogFactorAndVertexLight",
                "VARYINGS_NEED_FOG_AND_VERTEX_LIGHT", ShaderValueType.Float4,
                subscriptOptions: StructFieldOptions.Optional
            );
            public static readonly FieldDescriptor vsmDepth = new(Name, "vsmDepth",
                "", ShaderValueType.Float,
                preprocessor: "defined(_TOON_RP_VSM)",
                subscriptOptions: StructFieldOptions.Optional
            );
        }
    }
}