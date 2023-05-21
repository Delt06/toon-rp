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
            LowQuality,
            HighQuality,
        }

        [ToonRpShowIf(nameof(ShowBlurMessage), Mode = ToonRpShowIfAttribute.ShowIfMode.ShowHelpBox,
            HelpBoxMessage = "VSM blur requires a valid background. Make sure to add a shadow-casting ground mesh."
        )]
        public BlurMode Blur;
        public DirectionalShadows Directional;

        private bool ShowBlurMessage => Blur != BlurMode.None;

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