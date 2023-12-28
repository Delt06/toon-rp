namespace DELTation.ToonRP.Editor
{
    public static class ToonShaderUtils
    {
        public enum ShaderID
        {
            Unknown = -1,

            Default = 0,
            DefaultLite = 1,
            Unlit = 2,
            ParticlesUnlit = 3,
            InvertedHullOutline = 4,

            // ShaderGraph IDs start at 1000, correspond to subtargets
            SgUnlit = 1000, // ToonUnlitSubTarget
            SgDefault, // ToonDefaultSubTarget
            SgDefaultLite, // ToonDefaultLiteSubTarget
        }

        public static bool IsShaderGraph(this ShaderID id) => id >= ShaderID.SgUnlit;
    }
}