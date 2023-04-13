using UnityEngine.UIElements;

namespace DELTation.ToonRP.Editor
{
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