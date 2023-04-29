using DELTation.ToonRP.PostProcessing;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DELTation.ToonRP.Editor
{
    [CustomPropertyDrawer(typeof(ToonFxaaSettings))]
    public class ToonFxaaSettingsPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            SerializedProperty highQualityProperty =
                property.FindPropertyRelative(nameof(ToonFxaaSettings.HighQuality));
            var highQualityField = new PropertyField(highQualityProperty);
            var highQualityParams = new VisualElement();

            void RefreshFields()
            {
                highQualityParams.SetVisible(highQualityProperty.boolValue);
            }

            RefreshFields();

            highQualityField.RegisterValueChangeCallback(_ => RefreshFields());

            highQualityParams.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonFxaaSettings.FixedContrastThresholdId)))
            );
            highQualityParams.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonFxaaSettings.RelativeContrastThreshold)))
            );
            highQualityParams.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonFxaaSettings.SubpixelBlendingFactor)))
            );

            var foldout = new Foldout
            {
                text = "FXAA",
            };
            foldout.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonFxaaSettings.Enabled))));
            foldout.Add(highQualityField);
            foldout.Add(highQualityParams);
            root.Add(foldout);

            return root;
        }
    }
}