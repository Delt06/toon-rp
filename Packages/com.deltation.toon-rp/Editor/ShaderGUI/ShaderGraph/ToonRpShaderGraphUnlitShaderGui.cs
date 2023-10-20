using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Editor.ShaderGUI.ShaderGraph
{
    public class ToonRpShaderGraphUnlitShaderGui : ToonRpShaderGuiBase
    {
        protected override void DrawProperties()
        {
            DrawShaderGraphProperties(Properties);
        }

        protected override RenderQueue GetRenderQueue(Material m) => default;

        protected override bool ControlQueue => false;
    }
}