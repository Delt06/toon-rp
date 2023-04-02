using System;
using UnityEngine;

namespace ToonRP.Runtime.Shadows
{
    [Serializable]
    public struct ToonBlobShadowsSettings
    {
        public TextureSize AtlasSize;
        [Min(0f)]
        public float Saturation;
        public bool GPUInstancing;
    }
}