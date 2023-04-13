using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public static class MaterialExt
    {
        public static void SetKeyword(this Material material, string keyword, bool value)
        {
            material.SetKeyword(new LocalKeyword(material.shader, keyword), value);
        }
    }
}