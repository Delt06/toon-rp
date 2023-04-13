using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DELTation.ToonRP.Editor
{
    [CustomPropertyDrawer(typeof(ToonRpHeaderAttribute))]
    public class ToonRpHeaderPropertyDrawer : PropertyDrawer
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

            property.NextVisible(true);
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

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var toonRpHeaderAttribute = (ToonRpHeaderAttribute) attribute;
            var root = new VisualElement();
            root.Add(new ToonRpHeaderLabel(toonRpHeaderAttribute.Text ?? property.displayName));

            foreach (SerializedProperty child in GetDirectChildren(property))
            {
                root.Add(new PropertyField(child));
            }

            return root;
        }
    }
}