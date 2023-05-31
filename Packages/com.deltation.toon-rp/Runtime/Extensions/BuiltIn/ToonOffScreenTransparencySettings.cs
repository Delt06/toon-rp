using System;
using UnityEngine;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    [Serializable]
    public struct ToonOffScreenTransparencySettings
    {
        public enum TransparencyBlendMode
        {
            Alpha,
            Additive,
        }

        public string PassName;
        [Min(1)]
        public int ResolutionFactor;
        public bool AfterTransparent;
        public LayerMask LayerMask;
        [ColorUsage(true, true)]
        public Color Tint;
        public Texture2D Pattern;
        [Min(1)]
        public float PatternHorizontalTiling;
        public TransparencyBlendMode BlendMode;
    }
}