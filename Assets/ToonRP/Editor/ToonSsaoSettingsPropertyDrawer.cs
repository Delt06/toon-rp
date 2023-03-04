﻿using ToonRP.Runtime.PostProcessing;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ToonRP.Editor
{
    [CustomPropertyDrawer(typeof(ToonSsaoSettings))]
    public class ToonSsaoSettingsPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            SerializedProperty enabledProperty =
                property.FindPropertyRelative(nameof(ToonSsaoSettings.Enabled));
            var enabledField = new PropertyField(enabledProperty);
            var settingsContainer = new VisualElement();

            void RefreshFields()
            {
                settingsContainer.SetVisible(enabledProperty.boolValue);
            }

            RefreshFields();

            enabledField.RegisterValueChangeCallback(_ => RefreshFields());

            settingsContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonSsaoSettings.Radius)))
            );
            settingsContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonSsaoSettings.Power)))
            );
            settingsContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonSsaoSettings.KernelSize)))
            );
            settingsContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonSsaoSettings.HalfResolution)))
            );
            settingsContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonSsaoSettings.Threshold)))
            );
            settingsContainer.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonSsaoSettings.Smoothness)))
            );

            root.Add(new ToonRpHeaderLabel("SSAO"));
            root.Add(enabledField);
            root.Add(settingsContainer);

            return root;
        }
    }
}