using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ToonRP.Editor
{
    [CustomEditor(typeof(MonoBehaviour), true)]
    internal class ImguiToToolkitWrapper : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            UiElementsUtils.AddAllFields(serializedObject, root);
            return root;
        }
    }
}