using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Shadows.Blobs
{
    public sealed class DynamicSquareBlobShadowsMesh : DynamicBlobShadowsMesh<DynamicBlobShadowsMesh.VertexParams>
    {
        public override BlobShadowType ShadowType => BlobShadowType.Square;

        protected override VertexAttributeDescriptor[] VertexAttributeDescriptors => VertexAttributeDescriptorsParams;

        protected override VertexParams BuildVertex(Vector2 position, Vector2 uv, Vector4 @params) =>
            new()
            {
                Position = Vector2Half.FromVector2(position),
                Params = Vector4Half.FromVector4(@params),
                UV = Vector2Half.FromVector2(uv),
            };
    }
}