using DELTation.ToonRP.Shadows;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DELTation.ToonRP.Editor
{
    [CustomPropertyDrawer(typeof(ToonShadowSettings))]
    public class ToonShadowsSettingsPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            SerializedProperty modeProperty =
                property.FindPropertyRelative(nameof(ToonShadowSettings.Mode));
            SerializedProperty crispAntiAliasedProperty =
                property.FindPropertyRelative(nameof(ToonShadowSettings.CrispAntiAliased));
            var crispAntiAliasedField = new PropertyField(crispAntiAliasedProperty);
            var smoothnessField =
                new PropertyField(property.FindPropertyRelative(nameof(ToonShadowSettings.Smoothness)));
            var modeField = new PropertyField(modeProperty);

            var enabledContainer = new VisualElement();
            var vsmContainer = new VisualElement();
            var blobsContainer = new VisualElement();

            void RefreshFields()
            {
                var mode = (ToonShadowSettings.ShadowMode) modeProperty.intValue;
                enabledContainer.SetVisible(mode != ToonShadowSettings.ShadowMode.Off);
                vsmContainer.SetVisible(mode == ToonShadowSettings.ShadowMode.Vsm);
                blobsContainer.SetVisible(mode == ToonShadowSettings.ShadowMode.Blobs);
                smoothnessField.SetEnabled(!crispAntiAliasedProperty.boolValue);
            }

            RefreshFields();

            modeField.RegisterValueChangeCallback(_ => RefreshFields());
            crispAntiAliasedField.RegisterValueChangeCallback(_ => RefreshFields());

            root.Add(new ToonRpHeaderLabel("Shadows"));
            root.Add(modeField);

            // ramp
            {
                enabledContainer.Add(
                    new PropertyField(property.FindPropertyRelative(nameof(ToonShadowSettings.Threshold)))
                );
                enabledContainer.Add(crispAntiAliasedField);
                enabledContainer.Add(smoothnessField);
                enabledContainer.Add(
                    new PropertyField(property.FindPropertyRelative(nameof(ToonShadowSettings.MaxDistance)))
                );
                enabledContainer.Add(
                    new PropertyField(property.FindPropertyRelative(nameof(ToonShadowSettings.DistanceFade)))
                );
            }

            {
                vsmContainer.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonShadowSettings.Vsm)))
                    { label = "VSM" }
                );
                enabledContainer.Add(vsmContainer);
            }

            {
                blobsContainer.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonShadowSettings.Blobs)))
                    { label = "Blobs" }
                );
                enabledContainer.Add(blobsContainer);
            }


            root.Add(enabledContainer);
            return root;
        }
    }
}