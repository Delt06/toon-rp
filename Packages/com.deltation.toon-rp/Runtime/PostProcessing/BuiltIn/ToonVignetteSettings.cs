using System;
using DELTation.ToonRP.Attributes;
using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [Serializable]
    public struct ToonVignetteSettings
    {
        public bool Enabled;
        [ToonRpShowIf(nameof(Enabled))]
        [ColorUsage(false)]
        public Color Color;
        [ToonRpShowIf(nameof(Enabled))]
        [Range(0, 1)]
        public float CenterX;
        [ToonRpShowIf(nameof(Enabled))]
        [Range(0, 1)]
        public float CenterY;
        [ToonRpShowIf(nameof(Enabled))]
        [Range(0, 1)]
        public float Intensity;
        [ToonRpShowIf(nameof(Enabled))]
        [Range(0, 1)]
        public float Roundness;
        [ToonRpShowIf(nameof(Enabled))]
        [Range(0, 1)]
        public float Smoothness;
    }
}