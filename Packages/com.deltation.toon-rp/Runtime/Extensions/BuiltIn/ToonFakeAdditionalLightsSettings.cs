using System;
using UnityEngine;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    [Serializable]
    public struct ToonFakeAdditionalLightsSettings
    {
        public TextureSize Size;

        [Range(0.0f, 10.0f)]
        public float Intensity;

        [Range(0.0f, 1.0f)]
        public float Threshold;
        [Range(0.0f, 1.0f)]
        public float Smoothness;

        [Min(0.01f)]
        public float MaxDistance;
        [Range(0.001f, 1.0f)]
        public float DistanceFade;

        public float ReceiverPlaneY;
        [Min(0.01f)]
        public float MaxHeight;
        [Range(0.001f, 2.0f)]
        public float HeightFade;
    }
}