using System;
using UnityEngine;

namespace ToonRP.Runtime
{
    [Serializable]
    public struct ToonShadowSettings
    {
        public enum TextureSize
        {
            _256 = 256,
            _512 = 512,
            _1024 = 1024,
            _2048 = 2048,
            _4096 = 4096,
            _8192 = 8192,
        }

        [Min(0f)]
        public float MaxDistance;
        [Range(0.001f, 1f)]
        public float DistanceFade;
        public bool HighQualityBlur;
        public DirectionalShadows Directional;

        [Serializable]
        public struct DirectionalShadows
        {
            public bool Enabled;
            public TextureSize AtlasSize;
            [Range(0.0f, 1.0f)]
            public float Threshold;
            public bool CrispAntiAliased;
            [Range(0.0f, 1.0f)]
            public float Smoothness;
            [Range(0.0f, 2.0f)]
            public float DepthBias;
            [Range(0.0f, 2.0f)]
            public float NormalBias;
            [Range(0.0f, 20.0f)]
            public float SlopeBias;
        }
    }
}