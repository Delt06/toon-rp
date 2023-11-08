using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Shadows
{
    public sealed class DynamicCircleBlobShadowsMesh : DynamicBlobShadowsMesh<DynamicBlobShadowsMesh.Vertex>
    {
        public override BlobShadowType ShadowType => BlobShadowType.Circle;

        protected override VertexAttributeDescriptor[] VertexAttributeDescriptors => VertexAttributeDescriptorsDefault;

        protected override Vertex BuildVertex(Vector2 position, Vector2 uv, Vector4 @params) =>
            new()
            {
                Position = Vector2Half.FromVector2(position),
                UV = Vector2Half.FromVector2(uv),
            };
    }
}