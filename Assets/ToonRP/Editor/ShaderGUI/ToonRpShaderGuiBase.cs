using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ToonRP.Editor.ShaderGUI
{
    public abstract class ToonRpShaderGuiBase : UnityEditor.ShaderGUI
    {
        private MaterialEditor _materialEditor;
        private MaterialProperty[] Properties { get; set; }

        protected Material Material => (Material) _materialEditor.target;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            _materialEditor = materialEditor;
            Properties = properties;

            {
                EditorGUI.BeginChangeCheck();
                DrawProperties();

                EditorGUILayout.Space();

                materialEditor.EnableInstancingField();
                DrawQueueOffset();
                if (EditorGUI.EndChangeCheck())
                {
                    UpdateQueue();
                }
            }

            _materialEditor = null;
            Properties = null;
        }

        protected abstract void DrawProperties();

        protected bool DrawProperty(string propertyName, string labelOverride = null) =>
            DrawProperty(propertyName, out MaterialProperty _, labelOverride);

        protected bool DrawProperty(string propertyName, out MaterialProperty property, string labelOverride = null)
        {
            property = FindProperty(propertyName, Properties);
            EditorGUI.BeginChangeCheck();
            _materialEditor.ShaderProperty(property, labelOverride ?? property.displayName);
            bool changed = EditorGUI.EndChangeCheck();
            return changed;
        }

        protected MaterialProperty FindProperty(string propertyName) => FindProperty(propertyName, Properties);

        private void DrawQueueOffset()
        {
            MaterialProperty property = FindProperty(PropertyNames.QueueOffset, Properties);
            EditorGUI.showMixedValue = property.hasMixedValue;
            int currentValue = (int) property.floatValue;
            const int queueOffsetRange = 50;
            int newValue = EditorGUILayout.IntSlider("Queue Offset", currentValue, -queueOffsetRange, queueOffsetRange);
            if (currentValue != newValue)
            {
                property.floatValue = newValue;
                _materialEditor.PropertiesChanged();
            }

            EditorGUI.showMixedValue = false;
        }

        private void UpdateQueue()
        {
            RenderQueue renderQueue = GetRenderQueue();

            int queueOffset = (int) FindProperty(PropertyNames.QueueOffset, Properties).floatValue;
            Material.renderQueue = (int) renderQueue + queueOffset;
            Material.SetOverrideTag("RenderType", renderQueue switch
                {
                    RenderQueue.Background => "Opaque",
                    RenderQueue.Geometry => "Opaque",
                    RenderQueue.AlphaTest => "TransparentCutout",
                    RenderQueue.GeometryLast => "TransparentCutout",
                    RenderQueue.Transparent => "Transparent",
                    RenderQueue.Overlay => "Transparent",
                    _ => throw new ArgumentOutOfRangeException(),
                }
            );
        }

        protected abstract RenderQueue GetRenderQueue();
    }
}