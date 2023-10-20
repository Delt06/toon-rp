using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace DELTation.ToonRP.Editor.ShaderGraph
{
    internal static class ToonBlockFields
    {
        [GenerateBlocks]
        public struct VertexDescription
        {
            private const string Name = "ToonVertexDescription";

            public static readonly BlockFieldDescriptor Position = new(Name, "Position", "VERTEXDESCRIPTION_POSITION",
                new PositionControl(CoordinateSpace.Object), ShaderStage.Vertex
            );
            public static readonly BlockFieldDescriptor Normal = new(Name, "Normal", "VERTEXDESCRIPTION_NORMAL",
                new NormalControl(CoordinateSpace.Object), ShaderStage.Vertex
            );
            public static readonly BlockFieldDescriptor Tangent = new(Name, "Tangent", "VERTEXDESCRIPTION_TANGENT",
                new TangentControl(CoordinateSpace.Object), ShaderStage.Vertex
            );

            // public static readonly BlockFieldDescriptor Position = BlockFields.VertexDescription.Position;
            // public static readonly BlockFieldDescriptor Normal = BlockFields.VertexDescription.Normal;
            // public static readonly BlockFieldDescriptor Tangent = BlockFields.VertexDescription.Tangent;
        }

        [GenerateBlocks]
        public struct SurfaceDescription
        {
            private const string Name = "ToonSurfaceDescription";

            public static readonly BlockFieldDescriptor Albedo = new(Name, "Albedo", "Albedo",
                "SURFACEDESCRIPTION_ALBEDO",
                new ColorControl(Color.grey, false), ShaderStage.Fragment
            );
            public static readonly BlockFieldDescriptor NormalTs = new(Name, "NormalTS", "Normal (Tangent Space)",
                "SURFACEDESCRIPTION_NORMALTS",
                new NormalControl(CoordinateSpace.Tangent), ShaderStage.Fragment
            );
            public static readonly BlockFieldDescriptor NormalOs = new(Name, "NormalOS", "Normal (Object Space)",
                "SURFACEDESCRIPTION_NORMALOS",
                new NormalControl(CoordinateSpace.Object), ShaderStage.Fragment
            );
            public static readonly BlockFieldDescriptor NormalWs = new(Name, "NormalWS", "Normal (World Space)",
                "SURFACEDESCRIPTION_NORMALWS",
                new NormalControl(CoordinateSpace.World), ShaderStage.Fragment
            );

            public static BlockFieldDescriptor Emission = new(Name, "Emission", "SURFACEDESCRIPTION_EMISSION",
                new ColorControl(Color.black, true), ShaderStage.Fragment
            );
            public static readonly BlockFieldDescriptor Alpha = new(Name, "Alpha", "SURFACEDESCRIPTION_ALPHA",
                new FloatControl(1.0f), ShaderStage.Fragment
            );
            public static readonly BlockFieldDescriptor AlphaClipThreshold = new(Name, "AlphaClipThreshold",
                "Alpha Clip Threshold",
                "SURFACEDESCRIPTION_ALPHACLIPTHRESHOLD",
                new FloatControl(0.5f), ShaderStage.Fragment
            );
        }
    }
}