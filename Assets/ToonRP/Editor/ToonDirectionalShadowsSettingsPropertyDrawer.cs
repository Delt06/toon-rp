using ToonRP.Runtime;
using ToonRP.Runtime.Shadows;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ToonRP.Editor
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

            var enabledContainer = new VisualElement();

            void RefreshFields()
            {
                enabledContainer.SetEnabled(enabledProperty.boolValue);
            }

            RefreshFields();

            enabledField.RegisterValueChangeCallback(_ => RefreshFields());

            foldout.Add(enabledField);

            enabledContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonVsmShadowSettings.Directional.AtlasSize)))
            );
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