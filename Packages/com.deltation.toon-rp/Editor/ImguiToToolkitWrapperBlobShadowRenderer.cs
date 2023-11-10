using DELTation.ToonRP.Shadows;
using UnityEditor;

namespace DELTation.ToonRP.Editor
{
    [CustomEditor(typeof(BlobShadowRenderer), true)]
    [CanEditMultipleObjects]
    internal class ImguiToToolkitWrapperBlobShadowRenderer : ImguiToToolkitWrapper { }
}