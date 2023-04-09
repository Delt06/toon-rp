using System;
using UnityEngine;

namespace ToonRP.Runtime.PostProcessing
{
    [Serializable]
    public struct ToonInvertedHullOutlineSettings
    {
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
            public bool UseNormalsFromUV2;
            public float DepthBias;
            [Min(0f)]
            public float MaxDistance;
            [Range(0.001f, 1f)]
            public float DistanceFade;
        }
    }
}