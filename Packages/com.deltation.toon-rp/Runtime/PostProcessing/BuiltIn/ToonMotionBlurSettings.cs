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
        [Min(1)]
        public int NumSamples;
    }
}