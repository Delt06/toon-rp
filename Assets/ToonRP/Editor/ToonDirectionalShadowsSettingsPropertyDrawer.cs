using ToonRP.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ToonRP.Editor
{
    [CustomPropertyDrawer(typeof(ToonShadowSettings.DirectionalShadows))]
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
                property.FindPropertyRelative(nameof(ToonShadowSettings.Directional.Enabled));
            var enabledField = new PropertyField(enabledProperty);

            var enabledContainer = new VisualElement();

            void RefreshFields()
            {
                enabledContainer.SetEnabled(enabledProperty.boolValue);
            }

            enabledField.RegisterValueChangeCallback(_ => RefreshFields());
            foldout.Add(enabledField);

            enabledContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonShadowSettings.Directional.AtlasSize)))
            );
            enabledContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonShadowSettings.Directional.Threshold)))
            );
            enabledContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonShadowSettings.Directional.Smoothness)))
            );
            enabledContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonShadowSettings.Directional.DepthBias)))
            );
            enabledContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonShadowSettings.Directional.NormalBias)))
            );
            enabledContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonShadowSettings.Directional.SlopeBias)))
            );

            foldout.Add(enabledContainer);
            root.Add(foldout);
            return root;
        }
    }
}