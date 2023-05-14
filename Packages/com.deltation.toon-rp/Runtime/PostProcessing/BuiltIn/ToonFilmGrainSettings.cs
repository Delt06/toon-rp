using System;
using DELTation.ToonRP.Attributes;
using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [Serializable]
    public struct ToonFilmGrainSettings
    {
        public bool Enabled;
        [ToonRpShowIf(nameof(Enabled))]
        public Texture2D Texture;
        [ToonRpShowIf(nameof(Enabled))]
        [Range(0.0f, 1.0f)]
        public float Intensity;
        [ToonRpShowIf(nameof(Enabled))]
        [Min(0.0f)]
        public float LuminanceThreshold;
    }
}