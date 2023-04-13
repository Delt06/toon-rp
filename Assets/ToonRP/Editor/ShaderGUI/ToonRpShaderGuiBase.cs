using System;
using ToonRP.Editor.ShaderGUI.ShaderEnums;
using ToonRP.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using BlendMode = ToonRP.Editor.ShaderGUI.ShaderEnums.BlendMode;
using UnityBlendMode = UnityEngine.Rendering.BlendMode;

namespace ToonRP.Editor.ShaderGUI
{
    public abstract class ToonRpShaderGuiBase : UnityEditor.ShaderGUI
    {
        private const string ShadowCasterPassName = "ShadowCaster";
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

        protected void DrawAlphaClipping()
        {
            DrawProperty(PropertyNames.AlphaClipping, out MaterialProperty alphaClipping);
            if (alphaClipping.floatValue != 0)
            {
                DrawProperty(PropertyNames.AlphaClipThreshold);
            }
        }

        protected void DrawSurfaceProperties()
        {
            bool surfaceTypeChanged = DrawProperty(PropertyNames.SurfaceType);
            DrawAlphaClipping();
            SurfaceType surfaceTypeValue = GetSurfaceType();
            if (surfaceTypeValue == SurfaceType.Transparent)
            {
                if (DrawProperty(PropertyNames.BlendMode, out MaterialProperty blendMode) || surfaceTypeChanged)
                {
                    var blendModeValue = (BlendMode) blendMode.floatValue;
                    (UnityBlendMode blendSrc, UnityBlendMode blendDst) = blendModeValue switch
                    {
                        BlendMode.Alpha => (UnityBlendMode.SrcAlpha, UnityBlendMode.OneMinusSrcAlpha),
                        BlendMode.Premultiply => (UnityBlendMode.One, UnityBlendMode.OneMinusSrcAlpha),
                        BlendMode.Additive => (UnityBlendMode.One, UnityBlendMode.One),
                        BlendMode.Multiply => (UnityBlendMode.DstColor, UnityBlendMode.Zero),
                        _ => throw new ArgumentOutOfRangeException(),
                    };
                    SetBlend(blendSrc, blendDst);

                    Material.SetKeyword(ShaderKeywords.AlphaPremultiplyOn, blendModeValue == BlendMode.Premultiply);
                }

                if (surfaceTypeChanged)
                {
                    SetZWrite(false);
                    Material.SetShaderPassEnabled(ShadowCasterPassName, false);
                }
            }
            else if (surfaceTypeChanged)
            {
                SetBlend(UnityBlendMode.One, UnityBlendMode.Zero);
                SetZWrite(true);
                Material.DisableKeyword(ShaderKeywords.AlphaPremultiplyOn);
                Material.SetShaderPassEnabled(ShadowCasterPassName, true);
            }

            DrawProperty(PropertyNames.RenderFace);
        }

        protected SurfaceType GetSurfaceType() => (SurfaceType) FindProperty(PropertyNames.SurfaceType).floatValue;

        private void SetBlend(UnityBlendMode blendSrc, UnityBlendMode blendDst)
        {
            Material.SetFloat(PropertyNames.BlendSrc, (float) blendSrc);
            Material.SetFloat(PropertyNames.BlendDst, (float) blendDst);
        }

        private void SetZWrite(bool zWrite)
        {
            Material.SetFloat(PropertyNames.ZWrite, zWrite ? 1.0f : 0.0f);
            OnSetZWrite(zWrite);
        }

        protected bool IsZWriteOn() => Material.GetFloat(PropertyNames.ZWrite) > 0.5f;

        protected virtual void OnSetZWrite(bool zWrite) { }

        protected bool AlphaClippingEnabled() => FindProperty(PropertyNames.AlphaClipping).floatValue != 0;

        protected RenderQueue GetRenderQueueWithAlphaTestAndTransparency()
        {
            MaterialProperty surfaceType = FindProperty(PropertyNames.SurfaceType);
            return (SurfaceType) surfaceType.floatValue switch
            {
                SurfaceType.Opaque when AlphaClippingEnabled() => RenderQueue.AlphaTest,
                SurfaceType.Opaque => RenderQueue.Geometry,
                SurfaceType.Transparent => RenderQueue.Transparent,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
    }
}