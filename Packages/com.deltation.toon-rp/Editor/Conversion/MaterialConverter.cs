using System.Collections.Generic;
using System.Linq;
using DELTation.ToonRP.Editor.ShaderGUI;
using UnityEditor;
using UnityEngine;

namespace DELTation.ToonRP.Editor.Conversion
{
    internal static class MaterialConverter
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int CutoffId = Shader.PropertyToID("_Cutoff");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly HashSet<string> SupportedShaders = new()
        {
            "Standard",
            "Hidden/InternalErrorShader",
        };

        [MenuItem("Edit/Rendering/Materials/Convert Selected Built-in Materials to Toon RP")]
        private static void Convert()
        {
            Material[] materials = Selection.objects.OfType<Material>().ToArray();

            Undo.RecordObjects(materials.Cast<Object>().ToArray(), "Converted Materials to Toon RP");

            foreach (Material material in materials)
            {
                TryConvertToToonRP(material);
            }
        }

        public static bool TryConvertToToonRP(Material material)
        {
            Shader defaultShader = ToonRenderPipeline.GetDefaultShader();

            if (material.shader == defaultShader)
            {
                return true;
            }

            if (!SupportedShaders.Contains(material.shader.name))
            {
                Debug.LogWarning(
                    $"Could not convert {material}, its shader {material.shader} is not supported.",
                    material
                );
                return false;
            }

            Color color = material.GetColor(ColorId);
            Texture mainTex = material.GetTexture(MainTexId);
            Vector2 mainTexOffset = material.GetTextureOffset(MainTexId);
            Vector2 mainTexScale = material.GetTextureScale(MainTexId);
            float cutoff = material.GetFloat(CutoffId);
            Vector4 emissionColor = material.GetColor(EmissionColorId);

            material.SetTexture(MainTexId, null);

            material.shader = defaultShader;

            material.SetColor(PropertyNames.MainColor, color);
            material.SetTexture(PropertyNames.MainTexture, mainTex);
            material.SetTextureOffset(PropertyNames.MainTexture, mainTexOffset);
            material.SetTextureScale(PropertyNames.MainTexture, mainTexScale);
            material.SetFloat(PropertyNames.AlphaClipThreshold, cutoff);
            material.SetVector(PropertyNames.EmissionColor, emissionColor);

            EditorUtility.SetDirty(material);
            return true;
        }
    }
}