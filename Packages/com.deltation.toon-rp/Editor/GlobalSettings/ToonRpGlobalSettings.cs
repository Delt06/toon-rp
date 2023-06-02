using UnityEditor;
using UnityEngine;

namespace DELTation.ToonRP.Editor.GlobalSettings
{
    public class ToonRpGlobalSettings : ScriptableObject
    {
        private const string ParentPath = "Assets/Editor/";
        private const string Path = ParentPath + "Toon RP Global Settings.asset";

        public ShaderVariantStrippingMode ShaderVariantStrippingMode;

        public static ToonRpGlobalSettings GetOrCreateSettings()
        {
            ToonRpGlobalSettings settings = AssetDatabase.LoadAssetAtPath<ToonRpGlobalSettings>(Path);
            if (settings != null)
            {
                return settings;
            }

            settings = CreateInstance<ToonRpGlobalSettings>();
            settings.ShaderVariantStrippingMode = ShaderVariantStrippingMode.Always;
            if (!AssetDatabase.IsValidFolder(ParentPath))
            {
                AssetDatabase.CreateFolder("Assets", "Editor");
            }

            AssetDatabase.CreateAsset(settings, Path);
            AssetDatabase.SaveAssets();

            return settings;
        }

        internal static SerializedObject GetSerializedSettings() => new(GetOrCreateSettings());
    }
}