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

            AddProperty(settingsContainer, property, nameof(PatternSettings.Scale));
            AddProperty(settingsContainer, property, nameof(PatternSettings.Power));
            AddProperty(settingsContainer, property, nameof(PatternSettings.Multiplier));
            AddProperty(settingsContainer, property, nameof(PatternSettings.Smoothness));
            AddProperty(settingsContainer, property, nameof(PatternSettings.LuminanceThreshold));
            AddProperty(settingsContainer, property, nameof(PatternSettings.DotSizeLimit));
            AddProperty(settingsContainer, property, nameof(PatternSettings.Blend));
            AddProperty(settingsContainer, property, nameof(PatternSettings.FinalIntensityThreshold));

            root.Add(new ToonRpHeaderLabel("Pattern"));
            root.Add(enabledField);
            root.Add(settingsContainer);

            return root;
        }

        private static void AddProperty(VisualElement container, SerializedProperty property, string relativeName) =>
            container.Add(new PropertyField(property.FindPropertyRelative(relativeName)));
    }
}