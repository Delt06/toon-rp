using System;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public static class BlendModeExtensions
    {
        public static (BlendMode source, BlendMode destination)
            ToUnityBlendModes(this ToonBlendMode blendMode) =>
            blendMode switch
            {
                ToonBlendMode.Alpha => (BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha),
                ToonBlendMode.Premultiply => (BlendMode.One, BlendMode.OneMinusSrcAlpha),
                ToonBlendMode.Additive => (BlendMode.SrcAlpha, BlendMode.One),
                ToonBlendMode.Multiply => (BlendMode.DstColor, BlendMode.Zero),
                _ => throw new ArgumentOutOfRangeException(),
            };
    }
}