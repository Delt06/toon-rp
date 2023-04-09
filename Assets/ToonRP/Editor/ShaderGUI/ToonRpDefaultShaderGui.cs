using JetBrains.Annotations;
using ToonRP.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ToonRP.Editor.ShaderGUI
{
    [UsedImplicitly]
    public class ToonRpDefaultShaderGui : UnityEditor.ShaderGUI
    {
        private const string QueueOffset = "_QueueOffset";
        private const string AlphaClipping = "_AlphaClipping";
        private static readonly int ForwardStencilRefId = Shader.PropertyToID("_ForwardStencilRef");
        private static readonly int ForwardStencilWriteMaskId = Shader.PropertyToID("_ForwardStencilWriteMask");
        private static readonly int ForwardStencilCompId = Shader.PropertyToID("_ForwardStencilComp");
        private static readonly int ForwardStencilPassId = Shader.PropertyToID("_ForwardStencilPass");
        private MaterialEditor _materialEditor;
        private MaterialProperty[] _properties;

        private Material Material => (Material) _materialEditor.target;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            _materialEditor = materialEditor;
            _properties = properties;

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
            _properties = null;
        }

        private void DrawProperties()
        {
            DrawProperty("_MainColor");
            DrawProperty("_MainTexture");
            DrawProperty(AlphaClipping, out MaterialProperty alphaClipping);
            if (alphaClipping.floatValue != 0)
            {
                DrawProperty("_AlphaClipThreshold");
            }

            EditorGUILayout.Space();

            DrawProperty("_ShadowColor");
            DrawProperty("_SpecularColor");
            DrawProperty("_RimColor");

            EditorGUILayout.Space();

            DrawNormalMap();

            EditorGUILayout.Space();

            DrawProperty("_ReceiveBlobShadows");

            if (DrawProperty("_OutlinesStencilLayer", out MaterialProperty drawOutlines))
            {
                var stencilLayer = (StencilLayer) drawOutlines.floatValue;
                var outlinesStencilLayerKeyword = new LocalKeyword(Material.shader, "_HAS_OUTLINES_STENCIL_LAYER");

                if (stencilLayer != StencilLayer.None)
                {
                    byte reference = stencilLayer.ToReference();
                    Material.SetInteger(ForwardStencilRefId, reference);
                    Material.SetInteger(ForwardStencilWriteMaskId, reference);
                    Material.SetInteger(ForwardStencilCompId, (int) CompareFunction.Always);
                    Material.SetInteger(ForwardStencilPassId, (int) StencilOp.Replace);
                    Material.EnableKeyword(outlinesStencilLayerKeyword);
                }
                else
                {
                    Material.SetInteger(ForwardStencilRefId, 0);
                    Material.SetInteger(ForwardStencilWriteMaskId, 0);
                    Material.SetInteger(ForwardStencilCompId, 0);
                    Material.SetInteger(ForwardStencilPassId, 0);
                    Material.DisableKeyword(outlinesStencilLayerKeyword);
                }
            }

            EditorGUILayout.Space();

            DrawProperty("_OverrideRamp", out MaterialProperty overrideRamp);
            if (overrideRamp.floatValue != 0)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_OverrideRamp_Threshold");
                DrawProperty("_OverrideRamp_Smoothness");
                DrawProperty("_OverrideRamp_SpecularThreshold");
                DrawProperty("_OverrideRamp_SpecularSmoothness");
                DrawProperty("_OverrideRamp_RimThreshold");
                DrawProperty("_OverrideRamp_RimSmoothness");
                EditorGUI.indentLevel--;
            }
        }

        private void DrawNormalMap()
        {
            if (!DrawProperty("_NormalMap", out MaterialProperty normalMap))
            {
                return;
            }

            Material.SetKeyword(new LocalKeyword(Material.shader, "_NORMAL_MAP"), normalMap.textureValue != null);
            EditorUtility.SetDirty(Material);
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

        private void DrawQueueOffset()
        {
            MaterialProperty property = FindProperty(QueueOffset, _properties);
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
            bool alphaClipping = FindProperty(AlphaClipping, _properties).floatValue != 0;
            if (alphaClipping)
            {
                Material.renderQueue = (int) RenderQueue.AlphaTest;
                Material.SetOverrideTag("RenderType", "TransparentCutout");
            }
            else
            {
                Material.renderQueue = (int) RenderQueue.Geometry;
                Material.SetOverrideTag("RenderType", "Opaque");
            }

            int queueOffset = (int) FindProperty(QueueOffset, _properties).floatValue;
            Material.renderQueue += queueOffset;
        }
    }
}