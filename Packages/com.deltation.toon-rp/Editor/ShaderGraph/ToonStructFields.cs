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
                preprocessor: "!_FORCE_DISABLE_FOG || defined(_TOON_RP_ADDITIONAL_LIGHTS_VERTEX)",
                subscriptOptions: StructFieldOptions.Optional
            );
            public static readonly FieldDescriptor lightmapUv = new(Name, "lightmapUv",
                "VARYINGS_NEED_LIGHTMAP_UV", ShaderValueType.Float2,
                preprocessor: "defined(LIGHTMAP_ON)",
                subscriptOptions: StructFieldOptions.Optional
            );
            public static readonly FieldDescriptor vizUV = new(Name, "VizUV",
                "VARYINGS_NEED_VIZ_UV", ShaderValueType.Float2,
                preprocessor: "defined(EDITOR_VISUALIZATION)",
                subscriptOptions: StructFieldOptions.Optional
            );
            public static readonly FieldDescriptor lightCoord = new(Name, "LightCoord",
                "VARYINGS_NEED_LIGHT_COORD", ShaderValueType.Float4,
                preprocessor: "defined(EDITOR_VISUALIZATION)",
                subscriptOptions: StructFieldOptions.Optional
            );
            public static readonly FieldDescriptor vsmDepth = new(Name, "vsmDepth",
                "", ShaderValueType.Float,
                preprocessor: "defined(_TOON_RP_VSM)",
                subscriptOptions: StructFieldOptions.Optional
            );

            public static FieldDescriptor stereoTargetEyeIndexAsRTArrayIdx = new(Name,
                "stereoTargetEyeIndexAsRTArrayIdx", "", ShaderValueType.Uint,
                "SV_RenderTargetArrayIndex", "(defined(UNITY_STEREO_INSTANCING_ENABLED))", StructFieldOptions.Generated
            );
            public static FieldDescriptor stereoTargetEyeIndexAsBlendIdx0 = new(Name, "stereoTargetEyeIndexAsBlendIdx0",
                "", ShaderValueType.Uint,
                "BLENDINDICES0",
                "(defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))"
            );
        }
    }
}