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

            SerializedProperty crispAntiAliasedProperty =
                property.FindPropertyRelative(nameof(ToonRampSettings.CrispAntiAliased));
            var crispAntiAliasedField =
                new PropertyField(crispAntiAliasedProperty);
            var smoothnessField =
                new PropertyField(property.FindPropertyRelative(nameof(ToonRampSettings.Smoothness)));

            void RefreshSmoothness()
            {
                bool showSmoothness = !crispAntiAliasedProperty.boolValue;
                smoothnessField.SetEnabled(showSmoothness);
            }

            RefreshSmoothness();

            crispAntiAliasedField.RegisterValueChangeCallback(_ => RefreshSmoothness());

            root.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonRampSettings.Threshold))));
            root.Add(crispAntiAliasedField);
            root.Add(smoothnessField);
            root.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonRampSettings.SpecularThreshold))));
            root.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonRampSettings.SpecularSmoothness))));
            root.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonRampSettings.RimThreshold))));
            root.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonRampSettings.RimSmoothness))));

            return root;
        }
    }
}