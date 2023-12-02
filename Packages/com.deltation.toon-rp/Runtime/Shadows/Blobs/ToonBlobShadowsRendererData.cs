using Unity.Mathematics;
using UnityEngine;

namespace DELTation.ToonRP.Shadows.Blobs
{
    public struct ToonBlobShadowsRendererData
    {
        public float2 Position;
        public float HalfSize;
        public Bounds2D Bounds;
        public ToonBlobShadowType ShadowType;
        public Vector4 Params;
        public Texture2D BakedShadowTexture;
    }
}