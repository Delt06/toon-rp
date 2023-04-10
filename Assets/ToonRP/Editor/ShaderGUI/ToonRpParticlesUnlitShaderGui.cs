using JetBrains.Annotations;
using UnityEngine.Rendering;
using static ToonRP.Editor.ShaderGUI.PropertyNames;

namespace ToonRP.Editor.ShaderGUI
{
    [UsedImplicitly]
    public sealed class ToonRpParticlesUnlitShaderGui : ToonRpShaderGuiBase
    {
        protected override void DrawProperties()
        {
            DrawProperty(MainColor);
            DrawProperty(MainTexture);
        }

        protected override RenderQueue GetRenderQueue() => RenderQueue.Transparent;
    }
}