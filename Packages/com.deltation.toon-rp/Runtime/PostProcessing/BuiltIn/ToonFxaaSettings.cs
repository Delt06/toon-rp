using System;
using DELTation.ToonRP.Attributes;
using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [Serializable]
    public struct ToonFxaaSettings
    {
        public bool Enabled;
        [ToonRpShowIf(nameof(Enabled))]
        public bool HighQuality;
        [ToonRpShowIf(nameof(IsEnabledAndHighQuality))]
        [Min(0.0f)]
        public float FixedContrastThresholdId;
        [ToonRpShowIf(nameof(IsEnabledAndHighQuality))]
        [Min(0.0f)]
        public float RelativeContrastThreshold;
        [ToonRpShowIf(nameof(IsEnabledAndHighQuality))]
        [Range(0.0f, 1.0f)]
        public float SubpixelBlendingFactor;

        private bool IsEnabledAndHighQuality => Enabled && HighQuality;
    }
}