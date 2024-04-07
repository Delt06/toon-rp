using UnityEditor.ShaderGraph;

namespace DELTation.ToonRP.Editor.ShaderGraph
{
    internal static class ToonStructs
    {
        public static StructDescriptor Attributes = new()
        {
            name = "Attributes",
            packFields = false,
            fields = new[]
            {
                StructFields.Attributes.positionOS,
                StructFields.Attributes.normalOS,
                StructFields.Attributes.tangentOS,
                StructFields.Attributes.uv0,
                StructFields.Attributes.uv1,
                StructFields.Attributes.uv2,
                StructFields.Attributes.uv3,
                StructFields.Attributes.color,
                StructFields.Attributes.instanceID,
                StructFields.Attributes.weights,
                StructFields.Attributes.indices,
                StructFields.Attributes.vertexID,

                ToonStructFields.Attributes.positionOld,
            },
        };

        public static StructDescriptor Varyings = new()
        {
            name = "Varyings",
            packFields = true,
            populateWithCustomInterpolators = true,
            fields = new[]
            {
                StructFields.Varyings.positionCS,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,
                StructFields.Varyings.texCoord0,
                StructFields.Varyings.texCoord1,
                StructFields.Varyings.texCoord2,
                StructFields.Varyings.texCoord3,
                StructFields.Varyings.color,
                StructFields.Varyings.screenPosition,
                StructFields.Varyings.instanceID,
                ToonStructFields.Varyings.stereoTargetEyeIndexAsBlendIdx0,
                ToonStructFields.Varyings.stereoTargetEyeIndexAsRTArrayIdx,
                StructFields.Varyings.cullFace,

                ToonStructFields.Varyings.positionCsNoJitter,
                ToonStructFields.Varyings.previousPositionCsNoJitter,
                ToonStructFields.Varyings.fogFactorAndVertexLight,
                ToonStructFields.Varyings.lightmapUv,
                ToonStructFields.Varyings.vizUV,
                ToonStructFields.Varyings.lightCoord,
                ToonStructFields.Varyings.vsmDepth,
            },
        };
    }
}