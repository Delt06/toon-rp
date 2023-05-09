using System;
using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [Serializable]
    public struct ToonBloomSettings
    {
        [Min(0f)]
        public float Intensity;

        [Min(0f)]
        public float Threshold;

        [Range(0f, 1.0f)]
        public float ThresholdKnee;

        [Range(0, ToonBloom.MaxIterations)]
        public int MaxIterations;

        [Min(1)]
        public int ResolutionFactor;
        [Min(1)]
        public int DownsampleLimit;

        public bool BicubicUpsampling;

        public PatternSettings Pattern;

        [Serializable]
        public struct PatternSettings
        {
            public bool Enabled;
            [Min(0f)]
            public float Scale;
            [Min(0.001f)]
            public float Power;
            [Min(0f)]
            public float Multiplier;
            [Range(0.001f, 1.0f)]
            public float Smoothness;
        }
    }
}