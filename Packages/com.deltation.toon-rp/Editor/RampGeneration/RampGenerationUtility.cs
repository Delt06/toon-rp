using UnityEditor;
using UnityEngine;

namespace DELTation.ToonRP.Editor.RampGeneration
{
    public class RampGenerationUtility : EditorWindow
    {
        public Gradient Ramp;
        public int Size = 128;
        public FilterMode FilterMode = FilterMode.Bilinear;
        public bool MipMaps = true;

        private void OnGUI()
        {
            if (Ramp == null || Ramp.colorKeys.Length == 0)
            {
                Ramp = new Gradient
                {
                    colorKeys = new[]
                    {
                        new GradientColorKey(Color.black, 0.0f),
                        new GradientColorKey(Color.white, 0.5f),
                    },
                };
            }

            Ramp = EditorGUILayout.GradientField(nameof(Ramp), Ramp);
            Size = Mathf.Max(2, EditorGUILayout.IntField(nameof(Size), Size));
            FilterMode = (FilterMode) EditorGUILayout.EnumPopup("Filter Mode", FilterMode);
            MipMaps = EditorGUILayout.Toggle("Mip Maps", MipMaps);

            if (GUILayout.Button("Generate"))
            {
                var rampTexture = new Texture2D(Size, 1, TextureFormat.R8, MipMaps, true)
                {
                    name = "New Ramp",
                    wrapMode = TextureWrapMode.Clamp,
                };

                var pixels = new Color[Size];

                for (int i = 0; i < Size; i++)
                {
                    float t = (float) i / (Size - 1);
                    pixels[i] = Ramp.Evaluate(t);
                }

                rampTexture.SetPixels(pixels);
                rampTexture.Apply(MipMaps, false);

                CreateRampAsset(rampTexture);
            }
        }

        private static void CreateRampAsset(Texture2D texture)
        {
            string path = EditorUtility.SaveFilePanelInProject("Save mesh", texture.name, "asset",
                "Select ramp asset path"
            );
            if (string.IsNullOrEmpty(path))
            {
                DestroyImmediate(texture);
                return;
            }

            Texture2D existingTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (existingTexture == null)
            {
                AssetDatabase.CreateAsset(texture, path);
            }
            else
            {
                EditorUtility.CopySerialized(texture, existingTexture);
                DestroyImmediate(texture);
            }

            AssetDatabase.SaveAssets();

            Selection.activeObject = texture;
        }

        [MenuItem("Window/Toon RP/Ramp Generation Utility")]
        private static void OpenWindow()
        {
            RampGenerationUtility window = CreateWindow<RampGenerationUtility>();
            window.titleContent = new GUIContent("Ramp Generation Utility");
            window.ShowUtility();
        }
    }
}