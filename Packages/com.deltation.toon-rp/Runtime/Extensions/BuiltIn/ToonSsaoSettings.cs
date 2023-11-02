using System;
using UnityEngine;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    [Serializable]
    public struct ToonSsaoSettings
    {
        [Min(0.001f)]
        public float Radius;
        [Min(0.0001f)]
        public float Power;
        [Range(1, ToonSsao.MaxSamplesCount)]
        public int KernelSize;
        [Min(1)]
        public int ResolutionFactor;

        // Represents the opposite of the actual threshold, for more intuitive tweaking
        [Range(0.3f, 1.0f)]
        public float Threshold;
        [Range(0.0f, 1.0f)]
        public float Smoothness;

        public Texture2D Pattern;
        public Vector3 PatternScale;
    }
}