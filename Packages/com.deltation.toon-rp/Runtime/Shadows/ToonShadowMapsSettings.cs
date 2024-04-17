using System;
using DELTation.ToonRP.Attributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace DELTation.ToonRP.Shadows
{
    [Serializable]
    public struct ToonShadowMapsSettings
    {
        public enum BlurMode
        {
            None,
            Box,
            GaussianLowQuality,
            GaussianHighQuality,
        }

        public enum ShadowMapBits
        {
            _16 = 16,
            _24 = 24,
            _32 = 32,
        }

        public enum SoftShadowsMode
        {
            Poisson,
            PoissonStratified,
            PoissonRotated,
        }

        public enum SoftShadowsQuality
        {
            Low,
            High,
        }

        public enum VsmTexturePrecision
        {
            Float,
            Half,
        }

        public ShadowMapBits DepthBits;

        [ToonRpShowIf(nameof(IsBlurEnabled), Mode = ToonRpShowIfAttribute.ShowIfMode.ShowHelpBox,
            HelpBoxMessage = "VSM blur requires a valid background. Make sure to add a shadow-casting ground mesh."
        )]
        public BlurMode Blur;
        [ToonRpShowIf(nameof(IsBlurEarlyBailAllowed))]
        public bool BlurEarlyBail;
        [ToonRpShowIf(nameof(IsBlurEarlyBailEnabled))]
        [Min(0.000001f)]
        public float BlurEarlyBailThreshold;
        [ToonRpShowIf(nameof(IsBlurEnabled))]
        [Range(1.0f, 2.0f)]
        public float BlurScatter;
        [ToonRpShowIf(nameof(IsBlurEnabled))]
        public VsmTexturePrecision VsmPrecision;

        [ToonRpShowIf(nameof(IsBlurDisabled))]
        public SoftShadowsSettings SoftShadows;

        [ToonRpShowIf(nameof(IsBlurEnabled))]
        [Range(0.001f, 0.999f)]
        public float LightBleedingReduction;
        [ToonRpShowIf(nameof(IsBlurEnabled))]
        [Range(0.0f, 0.1f)]
        public float PrecisionCompensation;
        public DirectionalShadows Directional;
        public AdditionalShadows Additional;

        private bool IsBlurEnabled => Blur != BlurMode.None;
        public bool IsBlurEarlyBailAllowed => Blur == BlurMode.GaussianHighQuality;
        public bool IsBlurEarlyBailEnabled => IsBlurEarlyBailAllowed && BlurEarlyBail;

        private bool IsBlurDisabled => Blur == BlurMode.None;

        public ShadowMapBits GetShadowMapDepthBits() =>
            DepthBits == 0 ? ShadowMapBits._16 : DepthBits;

        [Serializable]
        public struct SoftShadowsSettings
        {
            [ToonRpHeader("Soft Shadows", Size = 12.0f)]
            public bool Enabled;

            [ToonRpShowIf(nameof(Enabled))]
            public SoftShadowsQuality Quality;
            [ToonRpShowIf(nameof(Enabled))]
            public SoftShadowsMode Mode;
            [ToonRpShowIf(nameof(EarlyBailAvailable))]
            public bool EarlyBail;

            [FormerlySerializedAs("PoissonSpread")]
            [ToonRpShowIf(nameof(Enabled))]
            [Range(0.0f, 2.0f)]
            public float Spread;

            [ToonRpShowIf(nameof(UsingRotatedPoisson))]
            public Texture3D RotatedPoissonSamplingTexture;

            private bool UsingRotatedPoisson => Enabled && Mode == SoftShadowsMode.PoissonRotated;
            private bool EarlyBailAvailable => Enabled && Quality == SoftShadowsQuality.High;
        }

        [Serializable]
        public struct DirectionalShadows
        {
            public bool Enabled;
            public TextureSize AtlasSize;
            [Range(1, ToonShadowMaps.MaxCascades)]
            public int CascadeCount;
            [Range(0f, 1f)]
            public float CascadeRatio1, CascadeRatio2, CascadeRatio3;
            [Range(0.0f, 2.0f)]
            public float DepthBias;
            [Range(-2.0f, 2.0f)]
            public float NormalBias;
            [Range(0.0f, 20.0f)]
            public float SlopeBias;

            public Vector3 GetRatios() => new(CascadeRatio1, CascadeRatio2, CascadeRatio3);
        }

        [Serializable]
        public struct AdditionalShadows
        {
            public bool Enabled;
            [ToonRpShowIf(nameof(Enabled))]
            public TextureSize AtlasSize;
        }
    }
}