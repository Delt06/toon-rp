using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Editor.ShaderGUI.ShaderGraph
{
    public class ToonRpShaderGraphDefaultShaderGui : ToonRpShaderGuiBase
    {
        protected override bool ControlQueue => false;

        protected override void DrawProperties()
        {
            DrawShaderGraphProperties(Properties);

            if (DrawFoldout("Built-In"))
            {
                DrawBlobShadows();
            }
        }

        protected override RenderQueue GetRenderQueue(Material m) => default;
    }
}