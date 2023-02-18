using ToonRP.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ToonRP.Editor
{
    [CustomPropertyDrawer(typeof(ToonRpHeaderAttribute))]
    public class ToonRpHeaderPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var toonRpHeaderAttribute = (ToonRpHeaderAttribute) attribute;
            var root = new VisualElement();
            root.Add(new ToonRpHeaderLabel(toonRpHeaderAttribute.Text ?? property.displayName));

            foreach (SerializedProperty child in property.Copy())
            {
                root.Add(new PropertyField(child));
            }

            return root;
        }
    }
}