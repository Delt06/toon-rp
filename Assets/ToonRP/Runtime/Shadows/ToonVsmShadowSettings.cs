using System;
using UnityEngine;

namespace ToonRP.Runtime.Shadows
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

        public BlurMode Blur;
        public DirectionalShadows Directional;

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