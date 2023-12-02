using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Shadows.Blobs
{
    public struct ToonBlobShadowsRendererData
    {
        public float2 Position;
        public float HalfSize;
        public Bounds2D Bounds;
        public ToonBlobShadowType ShadowType;
        public Vector4 Params;
        public RenderTargetIdentifier BakedShadowTexture;
    }
}