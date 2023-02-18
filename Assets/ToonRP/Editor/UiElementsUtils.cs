using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ToonRP.Editor
{
    public static class UiElementsUtils
    {
        public static void AddAllFields(SerializedObject source, VisualElement destination)
        {
            SerializedProperty prop = source.GetIterator();
            if (!prop.NextVisible(true))
            {
                return;
            }

            do
            {
                var field = new PropertyField(prop);

                if (prop.name == "m_Script")
                {
                    field.SetEnabled(false);
                }

                destination.Add(field);
            } while (prop.NextVisible(false));
        }
    }
}