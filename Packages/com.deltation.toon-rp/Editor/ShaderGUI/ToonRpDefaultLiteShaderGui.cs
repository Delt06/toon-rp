using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Editor.ShaderGUI
{
    [UsedImplicitly]
    public sealed class ToonRpDefaultLiteShaderGui : ToonRpShaderGuiBase
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
                DrawBlobShadows();
                DrawOverrideRamp();
                DrawProperty(PropertyNames.ForceDisableFog);
                DrawProperty(PropertyNames.ForceDisableEnvironmentLight);
            }

            EditorGUILayout.Space();
        }

        protected override void OnSetZWrite(bool zWrite)
        {
            base.OnSetZWrite(zWrite);
            ForEachMaterial(UpdateStencil);
        }

        protected override RenderQueue GetRenderQueue(Material m) => GetRenderQueueWithAlphaTestAndTransparency(m);
    }
}