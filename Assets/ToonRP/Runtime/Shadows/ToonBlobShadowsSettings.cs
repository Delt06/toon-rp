using System;
using UnityEngine;

namespace ToonRP.Runtime.Shadows
{
    [Serializable]
    public struct ToonBlobShadowsSettings
    {
        public TextureSize AtlasSize;
        public BlobShadowsMode Mode;
        [Min(0f)]
        public float Saturation;
        public bool GPUInstancing;
    }
}