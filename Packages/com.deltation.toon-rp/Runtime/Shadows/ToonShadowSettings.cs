using System;
using DELTation.ToonRP.Shadows.Blobs;
using UnityEngine;
using UnityEngine.Serialization;

namespace DELTation.ToonRP.Shadows
{
    [Serializable]
    public struct ToonShadowSettings
    {
        public enum ShadowMode
        {
            Off = 0,
            ShadowMapping,
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

        [FormerlySerializedAs("Vsm")] public ToonShadowMapsSettings ShadowMaps;
        public ToonBlobShadowsSettings Blobs;
    }
}