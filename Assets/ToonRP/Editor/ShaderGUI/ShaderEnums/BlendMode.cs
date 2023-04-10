namespace ToonRP.Editor.ShaderGUI.ShaderEnums
{
    public enum BlendMode
    {
        Alpha, // Old school alpha-blending mode, fresnel does not affect amount of transparency
        Premultiply, // Physically plausible transparency mode, implemented as alpha pre-multiply
        Additive,
        Multiply,
    }
}