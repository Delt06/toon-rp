using DELTation.ToonRP.Shadows.Blobs;
using UnityEditor;

namespace DELTation.ToonRP.Editor
{
    [CustomEditor(typeof(ToonBlobShadowRenderer), true)]
    [CanEditMultipleObjects]
    internal class ImguiToToolkitWrapperBlobShadowRenderer : ImguiToToolkitWrapper { }
}