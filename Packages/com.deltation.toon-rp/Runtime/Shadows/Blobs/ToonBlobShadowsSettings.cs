using System;
using UnityEngine;

namespace DELTation.ToonRP.Shadows.Blobs
{
    [Serializable]
    public struct ToonBlobShadowsSettings
    {
        public TextureSize AtlasSize;
        public ToonBlobShadowsMode Mode;
        [Min(0f)]
        public float Saturation;
        public Vector2 ShadowPositionOffset;
    }
}