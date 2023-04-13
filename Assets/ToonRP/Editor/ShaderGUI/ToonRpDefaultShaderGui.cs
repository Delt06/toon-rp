using JetBrains.Annotations;
using ToonRP.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ToonRP.Editor.ShaderGUI
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


        protected override void DrawProperties()
        {
            DrawSurfaceProperties();
            DrawOutlinesStencilLayer();

            EditorGUILayout.Space();

            DrawProperty(PropertyNames.MainColor);
            DrawProperty(PropertyNames.MainTexture);
            DrawProperty(PropertyNames.EmissionColor);

            EditorGUILayout.Space();

            DrawProperty(ShadowColorPropertyName);
            DrawProperty(SpecularColorPropertyName);
            DrawProperty(RimColorPropertyName);
            DrawNormalMap();

            EditorGUILayout.Space();

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
            EditorGUI.BeginDisabledGroup(!CanUseOutlinesStencilLayer());

            if (DrawProperty(OutlinesStencilLayerPropertyName))
            {
                UpdateStencil();
            }

            EditorGUI.EndDisabledGroup();
        }

        protected override void OnSetZWrite(bool zWrite)
        {
            base.OnSetZWrite(zWrite);
            UpdateStencil();
        }

        private void UpdateStencil()
        {
            var stencilLayer = (StencilLayer) FindProperty(OutlinesStencilLayerPropertyName).floatValue;

            if (stencilLayer != StencilLayer.None && CanUseOutlinesStencilLayer())
            {
                byte reference = stencilLayer.ToReference();
                Material.SetInteger(ForwardStencilRefId, reference);
                Material.SetInteger(ForwardStencilWriteMaskId, reference);
                Material.SetInteger(ForwardStencilCompId, (int) CompareFunction.Always);
                Material.SetInteger(ForwardStencilPassId, (int) StencilOp.Replace);
            }
            else
            {
                Material.SetInteger(ForwardStencilRefId, 0);
                Material.SetInteger(ForwardStencilWriteMaskId, 0);
                Material.SetInteger(ForwardStencilCompId, 0);
                Material.SetInteger(ForwardStencilPassId, 0);
            }
        }

        private bool CanUseOutlinesStencilLayer() => IsZWriteOn();

        private void DrawNormalMap()
        {
            if (DrawProperty(NormalMapPropertyName))
            {
                OnNormalMapUpdated();
            }
        }

        private void OnNormalMapUpdated()
        {
            Material.SetKeyword("_NORMAL_MAP", Material.GetTexture(NormalMapId) != null);
        }

        protected override RenderQueue GetRenderQueue() => GetRenderQueueWithAlphaTestAndTransparency();

        private void DrawButtons()
        {
            if (!GUILayout.Button("Disable Lighting"))
            {
                return;
            }

            Material.SetColor(PropertyNames.EmissionColor, Color.black);
            Material.SetColor(ShadowColorId, Color.clear);
            Material.SetColor(SpecularColorId, Color.black);
            Material.SetColor(RimColorId, Color.black);
            Material.SetTexture(NormalMapId, null);
            OnNormalMapUpdated();
        }
    }
}