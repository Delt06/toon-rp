using UnityEngine;

namespace DELTation.ToonRP.Editor.ShaderGUI
{
    public static class PropertyIds
    {
        public static readonly int ShadowColorId = Shader.PropertyToID(PropertyNames.ShadowColorPropertyName);
        public static readonly int SpecularColorId = Shader.PropertyToID(PropertyNames.SpecularColorPropertyName);
        public static readonly int RimColorId = Shader.PropertyToID(PropertyNames.RimColorPropertyName);
        public static readonly int NormalMapId = Shader.PropertyToID(PropertyNames.NormalMapPropertyName);
    }
}