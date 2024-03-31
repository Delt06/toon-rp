using System;
using DELTation.ToonRP.Editor.ShaderGraph.Targets;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Editor.ShaderGraph.AssetCallbacks
{
    internal static class CreateInvertedHullOutlineShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/Toon RP/Inverted Hull Outline Shader Graph",
            priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority + 3
        )]
        public static void CreateInvertedHullOutlineGraph()
        {
            var target = (ToonTarget) Activator.CreateInstance(typeof(ToonTarget));
            target.TrySetActiveSubTarget(typeof(ToonInvertedHullOutlineSubTarget));

            BlockFieldDescriptor[] blockDescriptors =
            {
                ToonBlockFields.VertexDescription.Position,
                ToonBlockFields.VertexDescription.Normal,
                ToonBlockFields.VertexDescription.OutlineThickness,
                ToonBlockFields.VertexDescription.DepthBias,

                ToonBlockFields.SurfaceDescription.Albedo,
                ToonBlockFields.SurfaceDescription.Emission,
            };

            GraphUtil.CreateNewGraphWithOutputs(new Target[] { target }, blockDescriptors);
        }
    }
}