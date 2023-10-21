namespace DELTation.ToonRP.Editor.ShaderGUI
{
    public static class PropertyNames
    {
        public const string MainColor = "_MainColor";
        public const string MainTexture = "_MainTexture";

        public const string SurfaceType = "_SurfaceType";
        public const string AlphaClipping = "_AlphaClipping";
        public const string AlphaClipThreshold = "_AlphaClipThreshold";
        public const string BlendMode = "_BlendMode";
        public const string BlendSrc = "_BlendSrc";
        public const string BlendDst = "_BlendDst";
        public const string ZWrite = "_ZWrite";
        public const string RenderFace = "_RenderFace";
        public const string EmissionColor = "_EmissionColor";

        public const string ShadowColorPropertyName = "_ShadowColor";
        public const string SpecularColorPropertyName = "_SpecularColor";
        public const string SpecularSizePropertyName = "_SpecularSizeOffset";
        public const string RimColorPropertyName = "_RimColor";
        public const string NormalMapPropertyName = "_NormalMap";

        public const string Specular = "_ToonLightingSpecular";
        public const string Rim = "_Rim";
        public const string ForceDisableFogPropertyName = "_ForceDisableFog";
        public const string ForceDisableEnvironmentLightPropertyName = "_ForceDisableEnvironmentLight";

        public const string CastShadows = "_CastShadows";
        public const string ReceiveShadows = "_ReceiveShadows";
        public const string ReceiveBlobShadows = "_ReceiveBlobShadows";

        // For ShaderGraph shaders only
        public const string ZTest = "_ZTest";
        public const string QueueOffset = "_QueueOffset";
        public const string ZWriteControl = "_ZWriteControl";
    }
}