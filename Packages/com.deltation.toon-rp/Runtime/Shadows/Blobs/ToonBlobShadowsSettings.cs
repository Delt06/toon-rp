using System;
using UnityEngine;

namespace DELTation.ToonRP.Shadows.Blobs
{
    [Serializable]
    public struct ToonBlobShadowsSettings
    {
        public TextureSize AtlasSize;
        public BlobShadowsMode Mode;
        [Min(0f)]
        public float Saturation;
        public Vector2 ShadowPositionOffset;
    }
}