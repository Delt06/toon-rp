using System;
using UnityEngine;

namespace ToonRP.Runtime.PostProcessing
{
    [Serializable]
    public struct ToonSsaoSettings
    {
        public bool Enabled;
        [Min(0.05f)]
        public float Radius;
        [Min(0.0001f)]
        public float Power;
        [Range(1, ToonSsao.MaxSamplesCount)]
        public int KernelSize;
        public bool HalfResolution;

        // Represents the opposite of the actual threshold, for more intuitive tweaking
        [Range(0.3f, 1.0f)]
        public float Threshold;
        [Range(0.0f, 1.0f)]
        public float Smoothness;
        public PatternSettings Pattern;

        [Serializable]
        public struct PatternSettings
        {
            public bool Enabled;
            public Vector3 Scale;
            [Range(0.0f, 1.0f)]
            public float Thickness;
            [Range(0.0f, 1.0f)]
            public float Smoothness;
            [Min(0f)]
            public float MaxDistance;
            [Range(0.001f, 1f)]
            public float DistanceFade;
        }
    }
}