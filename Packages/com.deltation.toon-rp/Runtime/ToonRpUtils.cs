#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define SET_MATERIAL_NAME
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public static class ToonRpUtils
    {
        [Conditional("SET_MATERIAL_NAME")]
        private static void SetMaterialName(Material material, string name)
        {
            material.name = name;
        }

        public static Material CreateEngineMaterial(Shader shader, string materialName)
        {
            Material material = CoreUtils.CreateEngineMaterial(shader);
            SetMaterialName(material, materialName);
            return material;
        }

        public static Material CreateEngineMaterial(string shaderPath, string materialName)
        {
            Material material = CoreUtils.CreateEngineMaterial(shaderPath);
            SetMaterialName(material, materialName);
            return material;
        }

        public static void ExecuteCommandBufferAndClear(ref this ScriptableRenderContext context, CommandBuffer cmd)
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}