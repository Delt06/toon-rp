using System;
using UnityEngine;

namespace DELTation.ToonRP.Shadows.Blobs
{
    [Serializable]
    public struct ToonBlobShadowSquareParams
    {
        [Range(0.0f, 1.0f)]
        public float Width;
        [Range(0.0f, 1.0f)]
        public float Height;
        [Range(0.0f, 1.0f)]
        public float CornerRadius;
        [Range(0.0f, 360.0f)]
        public float Rotation;
    }
}