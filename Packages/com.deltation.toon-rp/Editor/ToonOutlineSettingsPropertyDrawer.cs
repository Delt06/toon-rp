using DELTation.ToonRP.PostProcessing;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DELTation.ToonRP.Editor
{
    [CustomPropertyDrawer(typeof(ToonOutlineSettings))]
    public class ToonOutlineSettingsPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            var foldout = new Foldout
            {
                text = "Outline",
            };

            SerializedProperty modeProperty =
                property.FindPropertyRelative(nameof(ToonOutlineSettings.Mode));
            var modeField = new PropertyField(modeProperty);
            var invertedHullField =
                new PropertyField(property.FindPropertyRelative(nameof(ToonOutlineSettings.InvertedHull)));

            void RefreshFields()
            {
                var outlineMode = (ToonOutlineSettings.OutlineMode) modeProperty.intValue;
                invertedHullField.SetVisible(outlineMode == ToonOutlineSettings.OutlineMode.InvertedHull);
            }

            RefreshFields();

            modeField.RegisterValueChangeCallback(_ => RefreshFields());

            foldout.Add(modeField);
            foldout.Add(invertedHullField);
            root.Add(foldout);

            return root;
        }
    }
}