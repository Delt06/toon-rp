using System;
using DELTation.ToonRP.Attributes;
using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [Serializable]
    public struct ToonToneMappingSettings
    {
        public bool Enabled;
        [ToonRpShowIf(nameof(Enabled))]
        [Min(0.01f)]
        public float Exposure;
    }
}