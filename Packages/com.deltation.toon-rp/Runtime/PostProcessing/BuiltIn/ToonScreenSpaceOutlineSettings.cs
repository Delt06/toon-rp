using System;
using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [Serializable]
    public struct ToonScreenSpaceOutlineSettings
    {
        [ColorUsage(true, true)]
        public Color Color;

        public OutlineFilter ColorFilter;
        public OutlineFilter DepthFilter;
        public OutlineFilter NormalsFilter;

        public bool UseFog;
        [Min(0f)]
        public float MaxDistance;
        [Range(0.001f, 1f)]
        public float DistanceFade;

        [Serializable]
        public struct OutlineFilter
        {
            public bool Enabled;
            [Min(0.05f)]
            public float Threshold;
            [Min(0.0f)]
            public float Smoothness;
        }
    }
}