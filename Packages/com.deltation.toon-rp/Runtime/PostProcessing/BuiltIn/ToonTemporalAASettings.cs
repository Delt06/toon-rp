using System;
using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [Serializable]
    public struct ToonTemporalAASettings
    {
        [Min(0.0f)]
        public float JitterScale;
        [Range(0.0f, 1.0f)]
        public float ModulationFactor;
    }
}