using JetBrains.Annotations;
using UnityEditor;

namespace ToonRP.Editor.ShaderGUI
{
    [UsedImplicitly]
    public class ToonRpDefaultShaderGui : UnityEditor.ShaderGUI
    {
        private MaterialEditor _materialEditor;
        private MaterialProperty[] _properties;


        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            _materialEditor = materialEditor;
            _properties = properties;

            {
                DrawProperties();

                EditorGUILayout.Space();

                materialEditor.EnableInstancingField();
                materialEditor.RenderQueueField();
            }

            _materialEditor = null;
            _properties = null;
        }

        private void DrawProperties()
        {
            DrawProperty("_MainColor");
            DrawProperty("_MainTexture");
            DrawProperty("_ShadowColor");
            DrawProperty("_SpecularColor");
            
            EditorGUILayout.Space();
            
            DrawProperty("_ReceiveBlobShadows");

            EditorGUILayout.Space();

            DrawProperty("_OverrideRamp", out MaterialProperty overrideRamp);
            if (overrideRamp.floatValue != 0)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_OverrideRamp_Threshold");
                DrawProperty("_OverrideRamp_SpecularThreshold");
                DrawProperty("_OverrideRamp_Smoothness");
                DrawProperty("_OverrideRamp_SpecularSmoothness");
                EditorGUI.indentLevel--;
            }
        }

        private bool DrawProperty(string propertyName, string labelOverride = null) =>
            DrawProperty(propertyName, out MaterialProperty _, labelOverride);

        private bool DrawProperty(string propertyName, out MaterialProperty property, string labelOverride = null)
        {
            property = FindProperty(propertyName, _properties);
            EditorGUI.BeginChangeCheck();
            _materialEditor.ShaderProperty(property, labelOverride ?? property.displayName);
            bool changed = EditorGUI.EndChangeCheck();
            return changed;
        }
    }
}