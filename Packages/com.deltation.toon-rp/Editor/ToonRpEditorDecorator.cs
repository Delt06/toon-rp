using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DELTation.ToonRP.Editor
{
    public abstract class ToonRpEditorDecorator : PropertyDrawer
    {
        // https://forum.unity.com/threads/loop-through-serializedproperty-children.435119/
        // Answer by emrys90
        private static IEnumerable<SerializedProperty> GetDirectChildren(SerializedProperty property)
        {
            property = property.Copy();
            SerializedProperty nextElement = property.Copy();
            bool hasNextElement = nextElement.NextVisible(false);
            if (!hasNextElement)
            {
                nextElement = null;
            }

            if (property.NextVisible(true))
            {
                while (true)
                {
                    if (SerializedProperty.EqualContents(property, nextElement))
                    {
                        yield break;
                    }

                    yield return property;

                    bool hasNext = property.NextVisible(false);
                    if (!hasNext)
                    {
                        break;
                    }
                }
            }
        }

        public sealed override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            BeforeProperty(root, property);
            AddProperty(root, property);
            AfterProperty(root, property);

            return root;
        }

        protected virtual void BeforeProperty(VisualElement root, SerializedProperty property) { }

        protected virtual void AfterProperty(VisualElement root, SerializedProperty property) { }

        protected virtual VisualElement AddProperty(VisualElement root, SerializedProperty property)
        {
            var visualElement = new VisualElement();

            bool anyChildren = false;
            foreach (SerializedProperty child in GetDirectChildren(property))
            {
                visualElement.Add(new PropertyField(child));
                anyChildren = true;
            }

            if (!anyChildren)
            {
                visualElement.Add(new PropertyField(property));
            }

            root.Add(visualElement);
            return visualElement;
        }
    }
}