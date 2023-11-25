using System;
using UnityEngine;

namespace DELTation.ToonRP
{
    [Serializable]
    public struct ToonRampSettings
    {
        [Range(-1.0f, 1.0f)]
        public float Threshold;
        [Range(-1.0f, 1.0f)]
        public float SpecularThreshold;
        [Range(-1.0f, 1.0f)]
        public float RimThreshold;
        public ToonGlobalRampMode Mode;
        [Range(0.0f, 2.0f)]
        public float Smoothness;
        [Range(0.0f, 2.0f)]
        public float SpecularSmoothness;
        [Range(0.0f, 2.0f)]
        public float RimSmoothness;
        public Texture2D RampTexture;

        public AdditionalLightsRamp AdditionalLights;

        [Serializable]
        public struct AdditionalLightsRamp
        {
            [Range(-2.0f, 2.0f)]
            public float DiffuseOffset;
            [Range(-2.0f, 2.0f)]
            public float SpecularOffset;
            [Min(0.0f)]
            public float DistanceAttenuationFactor;
        }
    }
}