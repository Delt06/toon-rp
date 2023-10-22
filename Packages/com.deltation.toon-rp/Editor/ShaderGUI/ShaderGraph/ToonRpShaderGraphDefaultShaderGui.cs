namespace DELTation.ToonRP.Editor.ShaderGUI.ShaderGraph
{
    public class ToonRpShaderGraphDefaultShaderGui : ToonRpShaderGraphShaderGuiBase
    {
        protected override void DrawExtraBuiltInProperties()
        {
            base.DrawExtraBuiltInProperties();
            DrawBlobShadows();
        }
    }
}