using DELTation.ToonRP.Extensions;
using UnityEditor;

namespace DELTation.ToonRP.Editor
{
    [CustomEditor(typeof(ToonRenderingExtensionAsset), true)]
    [CanEditMultipleObjects]
    internal class ImguiToToolkitWrapperToonRenderingExtensionAsset : ImguiToToolkitWrapper { }
}