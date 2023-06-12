using System;
using DELTation.ToonRP.Attributes;
using UnityEngine;

namespace DELTation.ToonRP.Shadows
{
    [Serializable]
    public struct ToonVsmShadowSettings
    {
        public enum BlurMode
        {
            None,
            Box,
            GaussianLowQuality,
            GaussianHighQuality,
        }

        [ToonRpShowIf(nameof(IsBlurEnabled), Mode = ToonRpShowIfAttribute.ShowIfMode.ShowHelpBox,
            HelpBoxMessage = "VSM blur requires a valid background. Make sure to add a shadow-casting ground mesh."
        )]
        public BlurMode Blur;
        [ToonRpShowIf(nameof(IsBlurEarlyBailAllowed))]
        public bool BlurEarlyBail;
        [ToonRpShowIf(nameof(IsBlurEarlyBailEnabled))]
        [Min(0.000001f)]
        public float BlurEarlyBailThreshold;
        [ToonRpShowIf(nameof(IsBlurDisabled))]
        public bool SoftShadows;
        [ToonRpShowIf(nameof(IsBlurEnabled))]
        [Range(0.001f, 0.999f)]
        public float LightBleedingReduction;
        public DirectionalShadows Directional;

        private bool IsBlurEnabled => Blur != BlurMode.None;
        public bool IsBlurEarlyBailAllowed => Blur == BlurMode.GaussianHighQuality;
        public bool IsBlurEarlyBailEnabled => IsBlurEarlyBailAllowed && BlurEarlyBail;

        private bool IsBlurDisabled => Blur == BlurMode.None;

        [Serializable]
        public struct DirectionalShadows
        {
            public bool Enabled;
            public TextureSize AtlasSize;
            [Range(1, ToonVsmShadows.MaxCascades)]
            public int CascadeCount;
            [Range(0f, 1f)]
            public float CascadeRatio1, CascadeRatio2, CascadeRatio3;
            [Range(0.0f, 2.0f)]
            public float DepthBias;
            [Range(0.0f, 2.0f)]
            public float NormalBias;
            [Range(0.0f, 20.0f)]
            public float SlopeBias;

            public Vector3 GetRatios() => new(CascadeRatio1, CascadeRatio2, CascadeRatio3);
        }
    }
}