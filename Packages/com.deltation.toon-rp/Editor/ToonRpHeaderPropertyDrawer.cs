using DELTation.ToonRP.Attributes;
using UnityEditor;
using UnityEngine.UIElements;

namespace DELTation.ToonRP.Editor
{
    [CustomPropertyDrawer(typeof(ToonRpHeaderAttribute))]
    public class ToonRpHeaderPropertyDrawer : ToonRpEditorDecorator
    {
        protected override void BeforeProperty(VisualElement root, SerializedProperty property)
        {
            var toonRpHeaderAttribute = (ToonRpHeaderAttribute) attribute;
            root.Add(new ToonRpHeaderLabel(toonRpHeaderAttribute.Text ?? property.displayName,
                    toonRpHeaderAttribute.Size
                )
            );
        }
    }
}