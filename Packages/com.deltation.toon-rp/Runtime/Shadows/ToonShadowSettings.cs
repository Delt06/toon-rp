using System;
using UnityEngine;

namespace DELTation.ToonRP.Shadows
{
    [Serializable]
    public struct ToonShadowSettings
    {
        public enum ShadowMode
        {
            Off = 0,
            Vsm,
            Blobs,
        }

        public ShadowMode Mode;

        [Range(0.0f, 1.0f)]
        public float Threshold;
        public bool CrispAntiAliased;
        [Range(0.0f, 1.0f)]
        public float Smoothness;
        [Min(0f)]
        public float MaxDistance;
        [Range(0.001f, 1f)]
        public float DistanceFade;

        public Texture2D Pattern;
        public Vector3 PatternScale;

        public ToonVsmShadowSettings Vsm;
        public ToonBlobShadowsSettings Blobs;
    }
}