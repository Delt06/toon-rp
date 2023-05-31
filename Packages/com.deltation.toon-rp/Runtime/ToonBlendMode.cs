namespace DELTation.ToonRP
{
    public enum ToonBlendMode
    {
        Alpha, // Old school alpha-blending mode, fresnel does not affect amount of transparency
        Premultiply, // Physically plausible transparency mode, implemented as alpha pre-multiply
        Additive,
        Multiply,
    }
}