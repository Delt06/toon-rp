using System;
using UnityEngine;

namespace DELTation.ToonRP.PostProcessing
{
    [Serializable]
    public struct ToonFxaaSettings
    {
        public bool Enabled;
        [Min(0.0f)]
        public float FixedContrastThresholdId;
        [Min(0.0f)]
        public float RelativeContrastThreshold;
        [Range(0.0f, 1.0f)]
        public float SubpixelBlendingFactor;
    }
}