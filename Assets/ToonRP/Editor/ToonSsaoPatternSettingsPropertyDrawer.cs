using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using static ToonRP.Runtime.PostProcessing.ToonSsaoSettings;

namespace ToonRP.Editor
{
    [CustomPropertyDrawer(typeof(PatternSettings))]
    public class ToonSsaoPatternSettingsPropertyDrawer : PropertyDrawer
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
                new PropertyField(property.FindPropertyRelative(nameof(PatternSettings.Thickness)))
            );
            settingsContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(PatternSettings.Smoothness)))
            );
            settingsContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(PatternSettings.MaxDistance)))
            );
            settingsContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(PatternSettings.DistanceFade)))
            );

            root.Add(new ToonRpHeaderLabel("Pattern"));
            root.Add(enabledField);
            root.Add(settingsContainer);

            return root;
        }
    }
}