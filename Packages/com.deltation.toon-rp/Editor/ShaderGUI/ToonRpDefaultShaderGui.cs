using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Editor.ShaderGUI
{
    [UsedImplicitly]
    public sealed class ToonRpDefaultShaderGui : ToonRpShaderGuiBase
    {
        public override bool OutlinesStencilLayer => true;

        protected override void DrawProperties()
        {
            DrawSurfaceProperties();

            EditorGUILayout.Space();

            if (DrawFoldout(HeaderNames.Color))
            {
                DrawProperty(PropertyNames.MainColor);
                DrawProperty(PropertyNames.MainTexture);
            }


            EditorGUILayout.Space();

            if (DrawFoldout(HeaderNames.Lighting))
            {
                DrawProperty(PropertyNames.ShadowColorPropertyName);
                DrawProperty(PropertyNames.SpecularColorPropertyName);
                DrawProperty(PropertyNames.SpecularSizePropertyName);
                DrawProperty(PropertyNames.RimColorPropertyName);
                DrawProperty(PropertyNames.RimSizePropertyName);
                DrawProperty(PropertyNames.EmissionColor);
                DrawNormalMap();
                DrawBlobShadows();
                DrawOverrideRamp();
            }

            EditorGUILayout.Space();

            DrawButtons();
        }

        protected override void OnSetZWrite(bool zWrite)
        {
            base.OnSetZWrite(zWrite);
            ForEachMaterial(UpdateStencil);
        }


        private void DrawNormalMap()
        {
            if (DrawProperty(PropertyNames.NormalMapPropertyName))
            {
                OnNormalMapUpdated();
            }
        }

        private void OnNormalMapUpdated()
        {
            ForEachMaterial(m => m.SetKeyword(ShaderKeywords.NormalMap, m.GetTexture(PropertyIds.NormalMapId) != null));
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
                    m.SetColor(PropertyIds.ShadowColorId, Color.clear);
                    m.SetColor(PropertyIds.SpecularColorId, Color.black);
                    m.SetColor(PropertyIds.RimColorId, Color.black);
                    m.SetTexture(PropertyIds.NormalMapId, null);
                    EditorUtility.SetDirty(m);
                }
            );

            OnNormalMapUpdated();
        }
    }
}