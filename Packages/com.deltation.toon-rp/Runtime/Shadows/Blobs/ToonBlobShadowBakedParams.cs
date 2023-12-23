using System;
using UnityEngine;

namespace DELTation.ToonRP.Shadows.Blobs
{
    [Serializable]
    public struct ToonBlobShadowBakedParams
    {
        [Range(0, ToonBlobShadows.MaxBakedTextures - 1)]
        public int TextureIndex;
        [Range(0.0f, 360.0f)]
        public float Rotation;
    }
}