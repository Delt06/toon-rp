using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DELTation.ToonRP.Editor.GlobalSettings
{
    internal class ToonRpGlobalSettingsProvider : SettingsProvider
    {
        public const string Path = "Project/Graphics/Toon RP Global Settings";
        public const SettingsScope Scope = SettingsScope.Project;
        private SerializedObject _customSettings;

        private ToonRpGlobalSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _customSettings = ToonRpGlobalSettings.GetSerializedSettings();
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.PropertyField(
                _customSettings.FindProperty(nameof(ToonRpGlobalSettings.ShaderVariantStrippingMode)),
                Styles.ShaderVariantStrippingMode
            );

            _customSettings.ApplyModifiedProperties();
        }

        // Register the SettingsProvider
        [SettingsProvider]
        public static SettingsProvider CreateToonRpSettingsProvider()
        {
            var provider =
                new ToonRpGlobalSettingsProvider(Path, Scope)
                {
                    keywords = GetSearchKeywordsFromGUIContentProperties<Styles>(),
                };
            return provider;
        }

        private class Styles
        {
            public static readonly GUIContent ShaderVariantStrippingMode =
                new("Shader Variant Stripping");
        }
    }
}