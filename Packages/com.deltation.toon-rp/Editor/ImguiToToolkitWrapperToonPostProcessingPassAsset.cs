using DELTation.ToonRP.PostProcessing;
using UnityEditor;

namespace DELTation.ToonRP.Editor
{
    [CustomEditor(typeof(ToonPostProcessingPassAsset), true)]
    [CanEditMultipleObjects]
    internal class ImguiToToolkitWrapperToonPostProcessingPassAsset : ImguiToToolkitWrapper { }
}