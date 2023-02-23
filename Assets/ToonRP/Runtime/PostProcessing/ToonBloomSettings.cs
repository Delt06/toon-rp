using System;
using UnityEngine;

namespace ToonRP.Runtime.PostProcessing
{
    [Serializable]
    public struct ToonBloomSettings
    {
        public bool Enabled;

        [Min(0f)]
        public float Intensity;

        [Min(0f)]
        public float Threshold;

        [Range(0f, 1.0f)]
        public float ThresholdKnee;

        [Range(0, ToonBloom.MaxIterations)]
        public int MaxIterations;

        [Min(1)]
        public int DownsampleLimit;

        public bool BicubicUpsampling;
    }
}