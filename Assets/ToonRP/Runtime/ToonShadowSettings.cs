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
        public DirectionalShadows Directional;

        [Serializable]
        public struct DirectionalShadows
        {
            public TextureSize AtlasSize;
        }
    }
}