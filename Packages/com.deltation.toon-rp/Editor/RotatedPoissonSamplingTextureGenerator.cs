using UnityEditor;
using UnityEngine;

namespace DELTation.ToonRP.Editor
{
    public class RotatedPoissonSamplingTextureGenerator : EditorWindow
    {
        [SerializeField] [Min(1)]
        private int _size = 32;

        private void OnGUI()
        {
            _size = EditorGUILayout.IntField("Size", _size);

            if (GUILayout.Button("Generate"))
            {
                Generate();
            }
        }

        [MenuItem("Window/Toon RP/Rotated Poisson Sampling Texture Generator")]
        public static void Open()
        {
            RotatedPoissonSamplingTextureGenerator window = CreateWindow<RotatedPoissonSamplingTextureGenerator>();
            window.titleContent = new GUIContent("Rotated Poisson Sampling Texture Generator");
            window.Show();
        }

        private void Generate()
        {
            var texture = new Texture3D(_size, _size, _size, TextureFormat.RG16, false);
            Color[] pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                float rotation = Random.value * Mathf.PI * 2;
                float sin = Mathf.Sin(rotation);
                float cos = Mathf.Cos(rotation);
                ref Color pixel = ref pixels[i];

                pixel.r = Remap(cos);
                pixel.g = Remap(sin);
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);

            string path = EditorUtility.SaveFilePanelInProject(
                "Save texture",
                "RotatedPoissonSamplingTexture",
                "asset",
                "Save texture"
            );

            if (path.Length == 0)
            {
                return;
            }

            AssetDatabase.CreateAsset(texture, path);
            AssetDatabase.SaveAssets();
        }

        private static float Remap(float value) => (value + 1) * 0.5f;
    }
}