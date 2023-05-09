using System;
using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [Serializable]
    public struct ToonLightScatteringSettings
    {
        [Min(0.0f)]
        public float Threshold;
        [Range(0.0f, 0.999f)]
        public float BlurWidth;
        [Min(0.0f)]
        public float Intensity;
        [Min(10)]
        public int Samples;
        [Min(1)]
        public int ResolutionFactor;
        [Range(0.0f, 0.5f)]
        public float ScissorRadius;
    }
}