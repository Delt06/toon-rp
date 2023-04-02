using System;
using UnityEngine;

namespace ToonRP.Runtime.Shadows
{
    [Serializable]
    public struct ToonVsmShadowSettings
    {
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
            [Range(0.0f, 2.0f)]
            public float DepthBias;
            [Range(0.0f, 2.0f)]
            public float NormalBias;
            [Range(0.0f, 20.0f)]
            public float SlopeBias;
        }
    }
}