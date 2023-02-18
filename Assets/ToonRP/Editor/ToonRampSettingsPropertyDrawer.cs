using ToonRP.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ToonRP.Editor
{
    [CustomPropertyDrawer(typeof(ToonRampSettings))]
    public class ToonRampSettingsPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            root.Add(new ToonRpHeaderLabel("Global Ramp"));

            var thresholdField =
                new PropertyField(property.FindPropertyRelative(nameof(ToonRampSettings.Threshold)));
            SerializedProperty crispAntiAliasedProperty =
                property.FindPropertyRelative(nameof(ToonRampSettings.CrispAntiAliased));
            var crispAntiAliasedField =
                new PropertyField(crispAntiAliasedProperty);
            var smoothnessField =
                new PropertyField(property.FindPropertyRelative(nameof(ToonRampSettings.Smoothness)));
            var shadowColorField =
                new PropertyField(property.FindPropertyRelative(nameof(ToonRampSettings.ShadowColor)));

            void RefreshSmoothnessField()
            {
                smoothnessField.SetEnabled(!crispAntiAliasedProperty.boolValue);
            }

            RefreshSmoothnessField();

            crispAntiAliasedField.RegisterValueChangeCallback(_ => RefreshSmoothnessField());

            root.Add(thresholdField);
            root.Add(crispAntiAliasedField);
            root.Add(smoothnessField);
            root.Add(shadowColorField);

            return root;
        }
    }
}