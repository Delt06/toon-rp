using System;
using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [Serializable]
    public struct ToonMotionBlurSettings
    {
        public const int TargetFPS = 60;

        [Min(0.0f)]
        public float Strength;
        [Min(2)]
        public int NumSamples;
        [Range(0.0f, 1.0f)]
        public float WeightChangeRate;
    }
}