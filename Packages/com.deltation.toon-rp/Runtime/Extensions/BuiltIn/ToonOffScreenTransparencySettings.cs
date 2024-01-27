using System;
using DELTation.ToonRP.Attributes;
using UnityEngine;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    [Serializable]
    public struct ToonOffScreenTransparencySettings
    {
        public enum DepthDownsampleQualityLevel
        {
            Low,
            High,
        }

        public enum DepthRenderMode
        {
            PrePass,
            Downsample,
        }

        public enum TransparencyBlendMode
        {
            Alpha,
            Additive,
        }

        public const string DefaultPassName = "Off-Screen Transparency";

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
        public DepthRenderMode DepthMode;
        [ToonRpShowIf(nameof(IsDepthDownsampleEnabled))]
        public DepthDownsampleQualityLevel DepthDownsampleQuality;

        private bool IsDepthDownsampleEnabled => DepthMode == DepthRenderMode.Downsample;
    }
}