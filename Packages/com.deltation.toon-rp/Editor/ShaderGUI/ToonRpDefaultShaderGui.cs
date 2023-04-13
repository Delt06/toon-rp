using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Editor.ShaderGUI
{
    [UsedImplicitly]
    public sealed class ToonRpDefaultShaderGui : ToonRpShaderGuiBase
    {
        private const string OutlinesStencilLayerPropertyName = "_OutlinesStencilLayer";
        private const string ShadowColorPropertyName = "_ShadowColor";
        private const string SpecularColorPropertyName = "_SpecularColor";
        private const string RimColorPropertyName = "_RimColor";
        private const string NormalMapPropertyName = "_NormalMap";

        private static readonly int ForwardStencilRefId = Shader.PropertyToID("_ForwardStencilRef");
        private static readonly int ForwardStencilWriteMaskId = Shader.PropertyToID("_ForwardStencilWriteMask");
        private static readonly int ForwardStencilCompId = Shader.PropertyToID("_ForwardStencilComp");
        private static readonly int ForwardStencilPassId = Shader.PropertyToID("_ForwardStencilPass");
        private static readonly int ShadowColorId = Shader.PropertyToID(ShadowColorPropertyName);
        private static readonly int SpecularColorId = Shader.PropertyToID(SpecularColorPropertyName);
        private static readonly int RimColorId = Shader.PropertyToID(RimColorPropertyName);
        private static readonly int NormalMapId = Shader.PropertyToID(NormalMapPropertyName);
        private static readonly int OutlinesStencilLayerId = Shader.PropertyToID(OutlinesStencilLayerPropertyName);


        protected override void DrawProperties()
        {
            DrawSurfaceProperties();
            DrawOutlinesStencilLayer();

            EditorGUILayout.Space();

            DrawHeader(HeaderNames.Color);
            DrawProperty(PropertyNames.MainColor);
            DrawProperty(PropertyNames.MainTexture);

            EditorGUILayout.Space();

            DrawHeader(HeaderNames.Lighting);
            DrawProperty(ShadowColorPropertyName);
            DrawProperty(SpecularColorPropertyName);
            DrawProperty(RimColorPropertyName);
            DrawProperty(PropertyNames.EmissionColor);
            DrawNormalMap();
            DrawProperty("_ReceiveBlobShadows");
            DrawOverrideRamp();

            EditorGUILayout.Space();

            DrawButtons();
        }

        private void DrawOverrideRamp()
        {
            DrawProperty("_OverrideRamp", out MaterialProperty overrideRamp);
            if (overrideRamp.floatValue == 0)
            {
                return;
            }

            EditorGUI.indentLevel++;
            DrawProperty("_OverrideRamp_Threshold");
            DrawProperty("_OverrideRamp_Smoothness");
            DrawProperty("_OverrideRamp_SpecularThreshold");
            DrawProperty("_OverrideRamp_SpecularSmoothness");
            DrawProperty("_OverrideRamp_RimThreshold");
            DrawProperty("_OverrideRamp_RimSmoothness");
            EditorGUI.indentLevel--;
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

        protected override void OnSetZWrite(bool zWrite)
        {
            base.OnSetZWrite(zWrite);
            ForEachMaterial(UpdateStencil);
        }

        private void UpdateStencil(Material m)
        {
            var stencilLayer = (StencilLayer) m.GetFloat(OutlinesStencilLayerId);

            if (stencilLayer != StencilLayer.None && CanUseOutlinesStencilLayer(GetFirstMaterial()))
            {
                byte reference = stencilLayer.ToReference();
                m.SetInteger(ForwardStencilRefId, reference);
                m.SetInteger(ForwardStencilWriteMaskId, reference);
                m.SetInteger(ForwardStencilCompId, (int) CompareFunction.Always);
                m.SetInteger(ForwardStencilPassId, (int) StencilOp.Replace);
            }
            else
            {
                m.SetInteger(ForwardStencilRefId, 0);
                m.SetInteger(ForwardStencilWriteMaskId, 0);
                m.SetInteger(ForwardStencilCompId, 0);
                m.SetInteger(ForwardStencilPassId, 0);
            }
        }

        private static bool CanUseOutlinesStencilLayer(Material m) => IsZWriteOn(m);

        private bool IsCanUseOutlinesStencilLayerMixed() => FindProperty(PropertyNames.ZWrite).hasMixedValue;

        private void DrawNormalMap()
        {
            if (DrawProperty(NormalMapPropertyName))
            {
                OnNormalMapUpdated();
            }
        }

        private void OnNormalMapUpdated()
        {
            ForEachMaterial(m => m.SetKeyword("_NORMAL_MAP", m.GetTexture(NormalMapId) != null));
        }

        protected override RenderQueue GetRenderQueue(Material m) => GetRenderQueueWithAlphaTestAndTransparency(m);

        private void DrawButtons()
        {
            if (!GUILayout.Button("Disable Lighting"))
            {
                return;
            }

            Undo.RecordObjects(Targets, "Disable Lighting");

            ForEachMaterial(m =>
                {
                    m.SetColor(PropertyNames.EmissionColor, Color.black);
                    m.SetColor(ShadowColorId, Color.clear);
                    m.SetColor(SpecularColorId, Color.black);
                    m.SetColor(RimColorId, Color.black);
                    m.SetTexture(NormalMapId, null);
                    EditorUtility.SetDirty(m);
                }
            );

            OnNormalMapUpdated();
        }
    }
}