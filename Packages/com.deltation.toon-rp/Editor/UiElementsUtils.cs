using System;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DELTation.ToonRP.Editor
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

        public static void SetVisible([NotNull] this VisualElement visualElement, bool visible)
        {
            if (visualElement == null)
            {
                throw new ArgumentNullException(nameof(visualElement));
            }

            DisplayStyle styleDisplay = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visualElement.style.display != styleDisplay)
            {
                visualElement.style.display = styleDisplay;
            }
        }
    }
}