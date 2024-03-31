using System;
using DELTation.ToonRP.Editor.ShaderGraph.Targets;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Editor.ShaderGraph.AssetCallbacks
{
    internal static class CreateParticlesUnlitShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/Toon RP/Particles (Unlit) Shader Graph",
            priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority + 2
        )]
        public static void CreateUnlitGraph()
        {
            var target = (ToonTarget) Activator.CreateInstance(typeof(ToonTarget));
            target.TrySetActiveSubTarget(typeof(ToonParticlesUnlitSubTarget));

            BlockFieldDescriptor[] blockDescriptors =
            {
                ToonBlockFields.VertexDescription.Position,
                ToonBlockFields.VertexDescription.Normal,
                ToonBlockFields.VertexDescription.Tangent,
                ToonBlockFields.VertexDescription.DepthBias,
                
                ToonBlockFields.SurfaceDescription.Albedo,
                ToonBlockFields.SurfaceDescription.Emission,
            };

            GraphUtil.CreateNewGraphWithOutputs(new Target[] { target }, blockDescriptors);
        }
    }
}