using System;
using UnityEngine;

namespace DELTation.ToonRP.Shadows
{
    [Serializable]
    public struct ToonBlobShadowsSettings
    {
        public GameObject Model;
        public TextureSize AtlasSize;
        public BlobShadowsMode Mode;
        [Min(0f)]
        public float Saturation;
        public Vector2 ShadowPositionOffset;
    }
}