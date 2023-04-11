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
        private static readonly int ForwardStencilRefId = Shader.PropertyToID("_ForwardStencilRef");
        private static readonly int ForwardStencilWriteMaskId = Shader.PropertyToID("_ForwardStencilWriteMask");
        private static readonly int ForwardStencilCompId = Shader.PropertyToID("_ForwardStencilComp");
        private static readonly int ForwardStencilPassId = Shader.PropertyToID("_ForwardStencilPass");


        protected override void DrawProperties()
        {
            DrawProperty(PropertyNames.MainColor);
            DrawProperty(PropertyNames.MainTexture);
            DrawProperty(PropertyNames.EmissionColor);
            DrawAlphaClipping();

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

            Material.SetKeyword("_NORMAL_MAP", normalMap.textureValue != null);
            EditorUtility.SetDirty(Material);
        }

        protected override RenderQueue GetRenderQueue()
        {
            bool alphaClipping = AlphaClippingEnabled();
            return alphaClipping ? RenderQueue.AlphaTest : RenderQueue.Geometry;
        }
    }
}