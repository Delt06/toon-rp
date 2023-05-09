using DELTation.ToonRP.PostProcessing.BuiltIn;
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
            var screenSpaceField =
                new PropertyField(property.FindPropertyRelative(nameof(ToonOutlineSettings.ScreenSpace)));

            void RefreshFields()
            {
                var outlineMode = (ToonOutlineSettings.OutlineMode) modeProperty.intValue;
                invertedHullField.SetVisible(outlineMode == ToonOutlineSettings.OutlineMode.InvertedHull);
                screenSpaceField.SetVisible(outlineMode == ToonOutlineSettings.OutlineMode.ScreenSpace);
            }

            RefreshFields();

            modeField.RegisterValueChangeCallback(_ => RefreshFields());

            foldout.Add(modeField);
            foldout.Add(invertedHullField);
            foldout.Add(screenSpaceField);
            root.Add(foldout);

            return root;
        }
    }
}