using System.IO;
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
                // for compression
                const int height = 4;

                var rampTexture = new Texture2D(Size, height, TextureFormat.R8, MipMaps, true)
                {
                    name = "New Ramp",
                };

                var pixels = new Color[Size * height];

                for (int yi = 0; yi < height; yi++)
                {
                    for (int xi = 0; xi < Size; xi++)
                    {
                        float t = (float) xi / (Size - 1);
                        pixels[xi + yi * Size] = Ramp.Evaluate(t);
                    }
                }

                rampTexture.SetPixels(pixels);

                CreateRampAsset(rampTexture);
            }
        }

        private void CreateRampAsset(Texture2D texture)
        {
            string path = EditorUtility.SaveFilePanelInProject("Save mesh", texture.name, "png",
                "Select ramp asset path"
            );
            if (!string.IsNullOrEmpty(path))
            {
                byte[] bytes = texture.EncodeToPNG();
                File.WriteAllBytes(Path.Combine(Application.dataPath, "..", path), bytes);

                AssetDatabase.Refresh();

                var importer = (TextureImporter) AssetImporter.GetAtPath(path);
                importer.mipmapEnabled = MipMaps;
                importer.sRGBTexture = false;
                importer.wrapModeU = TextureWrapMode.Clamp;
                importer.wrapModeV = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode;
                importer.SaveAndReimport();

                AssetDatabase.SaveAssets();
            }

            DestroyImmediate(texture);
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