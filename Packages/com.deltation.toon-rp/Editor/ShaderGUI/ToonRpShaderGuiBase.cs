using System;
using System.Collections.Generic;
using DELTation.ToonRP.Editor.ShaderGUI.ShaderEnums;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using UnityBlendMode = UnityEngine.Rendering.BlendMode;

namespace DELTation.ToonRP.Editor.ShaderGUI
{
    public abstract class ToonRpShaderGuiBase : UnityEditor.ShaderGUI
    {
        private const string ShadowCasterPassName = "ShadowCaster";

        private const string OutlinesStencilLayerPropertyName = "_OutlinesStencilLayer";
        private static readonly Dictionary<string, bool> Foldouts = new();
        private static readonly int ForwardStencilRefId = Shader.PropertyToID("_ForwardStencilRef");
        private static readonly int ForwardStencilWriteMaskId = Shader.PropertyToID("_ForwardStencilWriteMask");
        private static readonly int ForwardStencilCompId = Shader.PropertyToID("_ForwardStencilComp");
        private static readonly int ForwardStencilPassId = Shader.PropertyToID("_ForwardStencilPass");
        private static readonly int OutlinesStencilLayerId = Shader.PropertyToID(OutlinesStencilLayerPropertyName);
        private GUIStyle _headerStyle;
        private MaterialEditor _materialEditor;

        private MaterialProperty[] Properties { get; set; }

        protected Object[] Targets => _materialEditor.targets;

        public virtual bool OutlinesStencilLayer => false;

        protected void ForEachMaterial(Action<Material> action)
        {
            foreach (Object target in Targets)
            {
                action((Material) target);
            }
        }

        protected Material GetFirstMaterial() => (Material) _materialEditor.target;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            _headerStyle = GUI.skin.label;
            _headerStyle.richText = true;

            _materialEditor = materialEditor;
            Properties = properties;

            {
                EditorGUI.BeginChangeCheck();
                DrawProperties();

                EditorGUILayout.Space();

                if (DrawFoldout(HeaderNames.Misc))
                {
                    materialEditor.EnableInstancingField();
                    DrawQueueOffset();
                    if (EditorGUI.EndChangeCheck())
                    {
                        UpdateQueue();
                    }
                }
            }

            _materialEditor = null;
            Properties = null;
        }

        protected static void DrawHeader(string text)
        {
            CoreEditorUtils.DrawHeader(text);
        }

        protected bool DrawFoldout(string text, bool openByDefault = true)
        {
            if (!Foldouts.TryGetValue(text, out bool wasOpen))
            {
                Foldouts[text] = wasOpen = openByDefault;
            }

            bool isOpenNow = CoreEditorUtils.DrawHeaderFoldout(text, wasOpen);
            Foldouts[text] = isOpenNow;
            return isOpenNow;
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
            ForEachMaterial(m =>
                {
                    RenderQueue renderQueue = GetRenderQueue(m);
                    int queueOffset = (int) m.GetFloat(PropertyNames.QueueOffset);
                    m.renderQueue = (int) renderQueue + queueOffset;
                    m.SetOverrideTag("RenderType", renderQueue switch
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
            );
        }

        protected abstract RenderQueue GetRenderQueue(Material m);

        private void DrawAlphaClipping()
        {
            DrawProperty(PropertyNames.AlphaClipping, out MaterialProperty alphaClipping);
            if (alphaClipping.floatValue != 0)
            {
                DrawProperty(PropertyNames.AlphaClipThreshold);
            }
        }

        protected bool DrawSurfaceProperties([CanBeNull] Action onFoldoutOpen = null)
        {
            bool isFoldoutOpen = DrawFoldout(HeaderNames.Surface);

            if (isFoldoutOpen)
            {
                bool surfaceTypeChanged =
                    DrawProperty(PropertyNames.SurfaceType, out MaterialProperty surfaceTypeProperty);
                DrawAlphaClipping();
                if (surfaceTypeProperty.hasMixedValue)
                {
                    return true;
                }

                SurfaceType surfaceTypeValue = GetSurfaceType(GetFirstMaterial());
                if (surfaceTypeValue == SurfaceType.Transparent)
                {
                    if (DrawProperty(PropertyNames.BlendMode, out MaterialProperty blendMode) || surfaceTypeChanged)
                    {
                        var blendModeValue = (ToonBlendMode) blendMode.floatValue;
                        (UnityBlendMode blendSrc, UnityBlendMode blendDst) = blendModeValue.ToUnityBlendModes();
                        SetBlend(blendSrc, blendDst);

                        ForEachMaterial(m =>
                            {
                                m.SetKeyword(ShaderKeywords.AlphaPremultiplyOn,
                                    blendModeValue == ToonBlendMode.Premultiply
                                );
                            }
                        );
                    }

                    if (surfaceTypeChanged)
                    {
                        SetZWrite(false);
                        ForEachMaterial(m => m.SetShaderPassEnabled(ShadowCasterPassName, false));
                    }
                }
                else if (surfaceTypeChanged)
                {
                    SetBlend(UnityBlendMode.One, UnityBlendMode.Zero);
                    SetZWrite(true);
                    ForEachMaterial(m =>
                        {
                            m.DisableKeyword(ShaderKeywords.AlphaPremultiplyOn);
                            m.SetShaderPassEnabled(ShadowCasterPassName, true);
                        }
                    );
                }

                if (surfaceTypeChanged)
                {
                    OnSurfaceTypeChanged();
                }

                DrawProperty(PropertyNames.RenderFace);

                if (OutlinesStencilLayer)
                {
                    DrawOutlinesStencilLayer();
                }

                onFoldoutOpen?.Invoke();
            }

            return isFoldoutOpen;
        }

        protected virtual void OnSurfaceTypeChanged() { }

        private static SurfaceType GetSurfaceType(Material m) => (SurfaceType) m.GetFloat(PropertyNames.SurfaceType);

        private void SetBlend(UnityBlendMode blendSrc, UnityBlendMode blendDst)
        {
            ForEachMaterial(m =>
                {
                    m.SetFloat(PropertyNames.BlendSrc, (float) blendSrc);
                    m.SetFloat(PropertyNames.BlendDst, (float) blendDst);
                }
            );
        }

        private void SetZWrite(bool zWrite)
        {
            ForEachMaterial(m => m.SetFloat(PropertyNames.ZWrite, zWrite ? 1.0f : 0.0f));

            if (OutlinesStencilLayer)
            {
                ForEachMaterial(UpdateStencil);
            }

            OnSetZWrite(zWrite);
        }

        protected static bool IsZWriteOn(Material m) => m.GetFloat(PropertyNames.ZWrite) > 0.5f;

        protected virtual void OnSetZWrite(bool zWrite) { }

        private bool AlphaClippingEnabled() => FindProperty(PropertyNames.AlphaClipping).floatValue != 0;

        protected RenderQueue GetRenderQueueWithAlphaTestAndTransparency(Material m)
        {
            return GetSurfaceType(m) switch
            {
                SurfaceType.Opaque when AlphaClippingEnabled() => RenderQueue.AlphaTest,
                SurfaceType.Opaque => RenderQueue.Geometry,
                SurfaceType.Transparent => RenderQueue.Transparent,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        private void DrawOutlinesStencilLayer()
        {
            if (IsCanUseOutlinesStencilLayerMixed())
            {
                return;
            }

            EditorGUI.BeginDisabledGroup(!CanUseOutlinesStencilLayer(GetFirstMaterial()));

            if (DrawProperty(OutlinesStencilLayerPropertyName))
            {
                ForEachMaterial(UpdateStencil);
            }

            EditorGUI.EndDisabledGroup();
        }

        protected void UpdateStencil(Material m)
        {
            var stencilLayer = (StencilLayer) m.GetFloat(OutlinesStencilLayerId);

            var hasOutlinesStencilLayerKeyword = new LocalKeyword(m.shader, "_HAS_OUTLINES_STENCIL_LAYER");
            if (stencilLayer != StencilLayer.None && CanUseOutlinesStencilLayer(GetFirstMaterial()))
            {
                byte reference = stencilLayer.ToReference();
                m.SetFloat(ForwardStencilRefId, reference);
                m.SetFloat(ForwardStencilWriteMaskId, reference);
                m.SetFloat(ForwardStencilCompId, (int) CompareFunction.Always);
                m.SetFloat(ForwardStencilPassId, (int) StencilOp.Replace);
                m.EnableKeyword(hasOutlinesStencilLayerKeyword);
            }
            else
            {
                m.SetFloat(ForwardStencilRefId, 0);
                m.SetFloat(ForwardStencilWriteMaskId, 0);
                m.SetFloat(ForwardStencilCompId, 0);
                m.SetFloat(ForwardStencilPassId, 0);
                m.DisableKeyword(hasOutlinesStencilLayerKeyword);
            }
        }

        private static bool CanUseOutlinesStencilLayer(Material m) => IsZWriteOn(m);

        private bool IsCanUseOutlinesStencilLayerMixed() => FindProperty(PropertyNames.ZWrite).hasMixedValue;
    }
}