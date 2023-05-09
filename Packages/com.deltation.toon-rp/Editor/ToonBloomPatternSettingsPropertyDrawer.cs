using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using static DELTation.ToonRP.PostProcessing.BuiltIn.ToonBloomSettings;

namespace DELTation.ToonRP.Editor
{
    [CustomPropertyDrawer(typeof(PatternSettings))]
    public class ToonBloomPatternSettingsPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            SerializedProperty enabledProperty =
                property.FindPropertyRelative(nameof(PatternSettings.Enabled));
            var enabledField = new PropertyField(enabledProperty);
            var settingsContainer = new VisualElement();

            void RefreshFields()
            {
                settingsContainer.SetVisible(enabledProperty.boolValue);
            }

            RefreshFields();

            enabledField.RegisterValueChangeCallback(_ => RefreshFields());

            settingsContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(PatternSettings.Scale)))
            );
            settingsContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(PatternSettings.Power)))
            );
            settingsContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(PatternSettings.Multiplier)))
            );
            settingsContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(PatternSettings.Smoothness)))
            );

            root.Add(new ToonRpHeaderLabel("Pattern"));
            root.Add(enabledField);
            root.Add(settingsContainer);

            return root;
        }
    }
}