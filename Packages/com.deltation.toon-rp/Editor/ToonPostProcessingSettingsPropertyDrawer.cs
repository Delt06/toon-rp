using DELTation.ToonRP.PostProcessing;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DELTation.ToonRP.Editor
{
    [CustomPropertyDrawer(typeof(ToonPostProcessingSettings))]
    public class ToonPostProcessingSettingsPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            SerializedProperty enabledProperty =
                property.FindPropertyRelative(nameof(ToonPostProcessingSettings.Enabled));
            var enabledField = new PropertyField(enabledProperty);
            var settingsContainer = new VisualElement();

            void RefreshFields()
            {
                settingsContainer.SetVisible(enabledProperty.boolValue);
            }

            RefreshFields();

            enabledField.RegisterValueChangeCallback(_ => RefreshFields());

            settingsContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonPostProcessingSettings.Fxaa)))
            );
            settingsContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonPostProcessingSettings.Bloom)))
            );
            settingsContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonPostProcessingSettings.Outline)))
            );

            root.Add(new ToonRpHeaderLabel("Post-Processing"));
            root.Add(enabledField);
            root.Add(settingsContainer);

            return root;
        }
    }
}