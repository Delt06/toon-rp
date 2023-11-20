using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DELTation.ToonRP.Editor
{
    [CustomPropertyDrawer(typeof(ToonRampSettings))]
    public class ToonRampSettingsPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            root.Add(new ToonRpHeaderLabel("Global Ramp"));

            SerializedProperty modeProperty =
                property.FindPropertyRelative(nameof(ToonRampSettings.Mode));
            var modeField = new PropertyField(modeProperty);
            var thresholdField = new PropertyField(property.FindPropertyRelative(nameof(ToonRampSettings.Threshold)));
            var rampTextureField =
                new PropertyField(property.FindPropertyRelative(nameof(ToonRampSettings.RampTexture)));
            var smoothnessField =
                new PropertyField(property.FindPropertyRelative(nameof(ToonRampSettings.Smoothness)));

            void Refresh()
            {
                var mode = (ToonGlobalRampMode) modeProperty.intValue;

                thresholdField.SetVisible(mode != ToonGlobalRampMode.Texture);
                rampTextureField.SetVisible(mode == ToonGlobalRampMode.Texture);
                smoothnessField.SetVisible(mode == ToonGlobalRampMode.Default);
            }

            Refresh();

            modeField.RegisterValueChangeCallback(_ => Refresh());

            root.Add(modeField);
            root.Add(thresholdField);
            root.Add(rampTextureField);
            root.Add(smoothnessField);
            root.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonRampSettings.SpecularThreshold))));
            root.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonRampSettings.SpecularSmoothness))));
            root.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonRampSettings.RimThreshold))));
            root.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonRampSettings.RimSmoothness))));
            
            root.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonRampSettings.AdditionalLights))));

            return root;
        }
    }
}