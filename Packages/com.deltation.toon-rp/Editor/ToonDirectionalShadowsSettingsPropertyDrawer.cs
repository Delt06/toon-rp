using DELTation.ToonRP.Shadows;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DELTation.ToonRP.Editor
{
    [CustomPropertyDrawer(typeof(ToonVsmShadowSettings.DirectionalShadows))]
    public class ToonDirectionalShadowsSettingsPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            var foldout = new Foldout
            {
                text = "Directional",
            };

            SerializedProperty enabledProperty =
                property.FindPropertyRelative(nameof(ToonVsmShadowSettings.Directional.Enabled));
            var enabledField = new PropertyField(enabledProperty);
            SerializedProperty cascadeCountProperty =
                property.FindPropertyRelative(nameof(ToonVsmShadowSettings.Directional.CascadeCount));
            var cascadeCountField = new PropertyField(cascadeCountProperty);

            var enabledContainer = new VisualElement();
            var cascadeRatio1 = new VisualElement();
            var cascadeRatio2 = new VisualElement();
            var cascadeRatio3 = new VisualElement();

            void RefreshFields()
            {
                enabledContainer.SetEnabled(enabledProperty.boolValue);

                int cascadeCount = cascadeCountProperty.intValue;
                cascadeRatio1.SetVisible(cascadeCount >= 2);
                cascadeRatio2.SetVisible(cascadeCount >= 3);
                cascadeRatio3.SetVisible(cascadeCount >= 4);
            }

            RefreshFields();

            enabledField.RegisterValueChangeCallback(_ => RefreshFields());
            cascadeCountField.RegisterValueChangeCallback(_ => RefreshFields());

            foldout.Add(enabledField);

            cascadeRatio1.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonVsmShadowSettings.Directional.CascadeRatio1))
                )
            );
            cascadeRatio2.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonVsmShadowSettings.Directional.CascadeRatio2))
                )
            );
            cascadeRatio3.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonVsmShadowSettings.Directional.CascadeRatio3))
                )
            );

            enabledContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonVsmShadowSettings.Directional.AtlasSize)))
            );

            enabledContainer.Add(cascadeCountField);
            enabledContainer.Add(cascadeRatio1);
            enabledContainer.Add(cascadeRatio2);
            enabledContainer.Add(cascadeRatio3);

            enabledContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonVsmShadowSettings.Directional.DepthBias)))
            );
            enabledContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonVsmShadowSettings.Directional.NormalBias)))
            );
            enabledContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonVsmShadowSettings.Directional.SlopeBias)))
            );

            foldout.Add(enabledContainer);
            root.Add(foldout);
            return root;
        }
    }
}