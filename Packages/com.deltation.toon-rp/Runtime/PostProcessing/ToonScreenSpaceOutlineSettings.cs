using System;
using UnityEngine;

namespace DELTation.ToonRP.PostProcessing
{
    [Serializable]
    public struct ToonScreenSpaceOutlineSettings
    {
        [ColorUsage(false, true)]
        public Color Color;

        public bool UseDepth;
        [Min(0.05f)]
        public float DepthThreshold;

        public bool UseNormals;
        [Min(0.05f)]
        public float NormalsThreshold;

        public bool UseColor;
        [Min(0.05f)]
        public float ColorThreshold;
        public bool UseFog;
    }
}