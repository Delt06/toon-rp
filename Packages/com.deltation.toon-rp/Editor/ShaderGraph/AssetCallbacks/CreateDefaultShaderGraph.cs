using System;
using DELTation.ToonRP.Editor.ShaderGraph.Targets;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Editor.ShaderGraph.AssetCallbacks
{
    internal static class CreateDefaultShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/Toon RP/Default Shader Graph",
            priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority
        )]
        public static void CreateLitGraph()
        {
            var target = (ToonTarget) Activator.CreateInstance(typeof(ToonTarget));
            target.TrySetActiveSubTarget(typeof(ToonDefaultSubTarget));

            BlockFieldDescriptor[] blockDescriptors =
            {
                ToonBlockFields.VertexDescription.Position,
                ToonBlockFields.VertexDescription.Normal,
                ToonBlockFields.VertexDescription.Tangent,
                ToonBlockFields.VertexDescription.DepthBias,

                ToonBlockFields.SurfaceDescription.Albedo,
                ToonBlockFields.SurfaceDescription.PositionWs,
                ToonBlockFields.SurfaceDescription.NormalTs,
                ToonBlockFields.SurfaceDescription.Emission,
                ToonBlockFields.SurfaceDescription.EmissionShadowBlend,
                ToonBlockFields.SurfaceDescription.GlobalRampUV,
                ToonBlockFields.SurfaceDescription.ShadowColor,
                ToonBlockFields.SurfaceDescription.DiffuseOffset,
                ToonBlockFields.SurfaceDescription.MainLightOcclusion,
                ToonBlockFields.SurfaceDescription.ShadowReceivePositionOffset,
                ToonBlockFields.SurfaceDescription.SpecularColor,
                ToonBlockFields.SurfaceDescription.SpecularSizeOffset,
                ToonBlockFields.SurfaceDescription.RimColor,
                ToonBlockFields.SurfaceDescription.RimSizeOffset,
            };

            GraphUtil.CreateNewGraphWithOutputs(new Target[] { target }, blockDescriptors);
        }
    }
}