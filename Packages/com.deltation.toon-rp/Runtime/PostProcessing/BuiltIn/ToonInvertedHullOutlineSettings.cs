using System;
using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [Serializable]
    public struct ToonInvertedHullOutlineSettings
    {
        public enum NormalsSource
        {
            Normals,
            UV2,
            Tangents,
        }

        public bool Enabled;

        public Pass[] Passes;

        [Serializable]
        public struct Pass
        {
            public string Name;
            public LayerMask LayerMask;
            public StencilLayer StencilLayer;
            [ColorUsage(false, true)]
            public Color Color;
            [Min(0f)]
            public float Thickness;
            [Min(0f)]
            public float NoiseAmplitude;
            [Min(0f)]
            public float NoiseFrequency;
            public NormalsSource NormalsSource;
            public float DepthBias;
            [Min(0f)]
            public float MaxDistance;
            [Range(0.001f, 1f)]
            public float DistanceFade;
        }
    }
}