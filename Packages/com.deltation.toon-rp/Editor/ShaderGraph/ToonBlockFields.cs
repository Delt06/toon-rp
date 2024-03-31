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
            public static readonly BlockFieldDescriptor DepthBias = new(Name, "DepthBias", "Depth Bias",
                "VERTEXDESCRIPTION_DEPTHBIAS",
                new FloatControl(0.0f), ShaderStage.Vertex
            );

            public static readonly BlockFieldDescriptor BillboardCameraPull = new(Name, "BillboardCameraPull",
                "Billboard Camera Pull",
                "VERTEXDESCRIPTION_BILLBOARDCAMERAPULL",
                new FloatControl(0.0f), ShaderStage.Vertex
            );
            public static readonly BlockFieldDescriptor OutlineThickness = new(Name, "OutlineThickness",
                "VERTEXDESCRIPTION_OUTLINE_THICKNESS",
                new FloatControl(1.0f), ShaderStage.Vertex
            );
        }

        [GenerateBlocks]
        public struct SurfaceDescription
        {
            private const string Name = "ToonSurfaceDescription";

            public static readonly BlockFieldDescriptor Albedo = new(Name, "Albedo", "Albedo",
                "SURFACEDESCRIPTION_ALBEDO",
                new ColorControl(Color.grey, false), ShaderStage.Fragment
            );

            public static readonly BlockFieldDescriptor PositionWs = new(Name, "PositionWS", "Position (World Space)",
                "SURFACEDESCRIPTION_POSITIONWS",
                new PositionControl(CoordinateSpace.World), ShaderStage.Fragment
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

            public static readonly BlockFieldDescriptor Emission = new(Name, "Emission", "SURFACEDESCRIPTION_EMISSION",
                new ColorControl(Color.black, true), ShaderStage.Fragment
            );
            public static readonly BlockFieldDescriptor EmissionShadowBlend = new(Name, "EmissionShadowBlend",
                "Emission Shadow Blend",
                "SURFACEDESCRIPTION_EMISSIONSHADOWBLEND",
                new FloatControl(1.0f), ShaderStage.Fragment
            );

            public static readonly BlockFieldDescriptor Alpha = new(Name, "Alpha", "SURFACEDESCRIPTION_ALPHA",
                new FloatControl(1.0f), ShaderStage.Fragment
            );
            public static readonly BlockFieldDescriptor AlphaClipThreshold = new(Name, "AlphaClipThreshold",
                "Alpha Clip Threshold",
                "SURFACEDESCRIPTION_ALPHACLIPTHRESHOLD",
                new FloatControl(0.5f), ShaderStage.Fragment
            );

            public static readonly BlockFieldDescriptor CustomFogFactor = new(Name, "CustomFogFactor",
                "Custom Fog Factor", "SURFACEDESCRIPTION_CUSTOMFOGFACTOR",
                new FloatControl(0.0f), ShaderStage.Fragment
            );
            public static readonly BlockFieldDescriptor CustomFogColor = new(Name, "CustomFogColor", "Custom Fog Color",
                "SURFACEDESCRIPTION_CUSTOMFOGCOLOR",
                new ColorControl(Color.grey, false), ShaderStage.Fragment
            );

            public static readonly BlockFieldDescriptor GlobalRampUV = new(Name, "GlobalRampUV", "Global Ramp UV",
                "SURFACEDESCRIPTION_GLOBALRAMPUV",
                new Vector2Control(Vector2.zero), ShaderStage.Fragment
            );

            public static readonly BlockFieldDescriptor SpecularColor = new(Name, "SpecularColor", "Specular Color",
                "SURFACEDESCRIPTION_SPECULARCOLOR",
                new ColorControl(Color.white, true), ShaderStage.Fragment
            );

            public static readonly BlockFieldDescriptor SpecularSizeOffset = new(Name, "SpecularSizeOffset",
                "Specular Size Offset",
                "SURFACEDESCRIPTION_SPECULARSIZEOFFSET",
                new FloatControl(0.0f), ShaderStage.Fragment
            );

            public static readonly BlockFieldDescriptor ShadowColor = new(Name, "ShadowColor", "Shadow Color",
                "SURFACEDESCRIPTION_SHADOWCOLOR",
                new ColorRGBAControl(new Color(0.0f, 0.0f, 0.0f, 0.75f)), ShaderStage.Fragment
            );

            public static readonly BlockFieldDescriptor DiffuseOffset = new(Name, "DiffuseOffset",
                "Diffuse Offset",
                "SURFACEDESCRIPTION_DIFFUSEOFFSET",
                new FloatControl(0.0f), ShaderStage.Fragment
            );

            public static readonly BlockFieldDescriptor MainLightOcclusion = new(Name, "MainLightOcclusion",
                "Main Light Occlusion",
                "SURFACEDESCRIPTION_MAINLIGHTOCCLUSION",
                new FloatControl(1.0f), ShaderStage.Fragment
            );

            public static readonly BlockFieldDescriptor ShadowReceivePositionOffset = new(Name,
                "ShadowReceivePositionOffset",
                "Shadow Receive Position Offset",
                "SURFACEDESCRIPTION_SHADOWRECEIVEPOSITIONOFFSET",
                new Vector3Control(Vector3.zero), ShaderStage.Fragment
            );

            public static readonly BlockFieldDescriptor RimColor = new(Name, "RimColor", "Rim Color",
                "SURFACEDESCRIPTION_RIMCOLOR",
                new ColorControl(Color.white, true), ShaderStage.Fragment
            );

            public static readonly BlockFieldDescriptor RimSizeOffset = new(Name, "RimSizeOffset", "Rim Size Offset",
                "SURFACEDESCRIPTION_RIMSIZEOFFSET",
                new FloatControl(0.0f), ShaderStage.Fragment
            );

            public static readonly BlockFieldDescriptor OverrideRampThreshold = new(Name, "OverrideRampThreshold",
                "Override Ramp Threshold",
                "SURFACEDESCRIPTION_OVERRIDERAMPTHRESHOLD",
                new FloatControl(0.0f), ShaderStage.Fragment
            );
            public static readonly BlockFieldDescriptor OverrideRampSmoothness = new(Name, "OverrideRampSmoothness",
                "Override Ramp Smoothness",
                "SURFACEDESCRIPTION_OVERRIDERAMPSMOOTHNESS",
                new FloatControl(0.083f), ShaderStage.Fragment
            );
            public static readonly BlockFieldDescriptor OverrideRampSpecularThreshold = new(Name,
                "OverrideRampSpecularThreshold", "Override Ramp Specular Threshold",
                "SURFACEDESCRIPTION_OVERRIDERAMPSPECULARTHRESHOLD",
                new FloatControl(0.095f), ShaderStage.Fragment
            );
            public static readonly BlockFieldDescriptor OverrideRampSpecularSmoothness = new(Name,
                "OverrideRampSpecularSmoothness", "Override Ramp Specular Smoothness",
                "SURFACEDESCRIPTION_OVERRIDERAMPSPECULARSMOOTHNESS",
                new FloatControl(0.005f), ShaderStage.Fragment
            );
            public static readonly BlockFieldDescriptor OverrideRampRimThreshold = new(Name, "OverrideRampRimThreshold",
                "Override Ramp Rim Threshold",
                "SURFACEDESCRIPTION_OVERRIDERAMPRIMTHRESHOLD",
                new FloatControl(0.5f), ShaderStage.Fragment
            );
            public static readonly BlockFieldDescriptor OverrideRampRimSmoothness = new(Name,
                "OverrideRampRimSmoothness", "Override Ramp Rim Smoothness",
                "SURFACEDESCRIPTION_OVERRIDERAMPRIMSMOOTHNESS",
                new FloatControl(0.1f), ShaderStage.Fragment
            );

            public static readonly BlockFieldDescriptor SoftParticlesDistance = new(Name, "SoftParticlesDistance",
                "Soft Particles Distance",
                "SURFACEDESCRIPTION_SOFTPARTICLESDISTANCE",
                new FloatControl(0.0f), ShaderStage.Fragment
            );
            public static readonly BlockFieldDescriptor SoftParticlesRange = new(Name, "SoftParticlesRange",
                "Soft Particles Range",
                "SURFACEDESCRIPTION_SOFTPARTICLESRANGE",
                new FloatControl(1.0f), ShaderStage.Fragment
            );
        }
    }
}