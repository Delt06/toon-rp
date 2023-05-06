using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Editor.ShaderGUI
{
    [UsedImplicitly]
    public sealed class ToonRpUnlitShaderGui : ToonRpShaderGuiBase
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
        }

        protected override RenderQueue GetRenderQueue(Material m) => GetRenderQueueWithAlphaTestAndTransparency(m);
    }
}