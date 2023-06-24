using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace DELTation.ToonRP.Editor.VertexColorPaint
{
    public class VertexColorPaintEditorWindow : EditorWindow
    {
        private static readonly Color32 White = new(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
        [SerializeField] private VertexColorPaintImporter _importer;
        [SerializeField] private Vector3 _position;
        [SerializeField] private Vector3 _rotation;
        [SerializeField] private Vector3 _scale = Vector3.one;
        [SerializeField] private Mesh _mesh;
        [SerializeField] private Color32[] _colors;

        private Material _material;
        private PreviewRenderUtility _renderer;
        private SerializedObject _serializedObject;

        private void OnEnable()
        {
            _renderer ??= CreateRenderer();
            _serializedObject = new SerializedObject(this);

            _material = ToonRpUtils.CreateEngineMaterial("Hidden/Vertex Color Paint", "Vertex Color Paint");
        }

        private void OnDisable()
        {
            if (_material != null)
            {
                DestroyImmediate(_material);
            }

            _renderer?.Cleanup();
        }

        private void OnGUI()
        {
            if (_importer == null)
            {
                return;
            }

            if (_mesh == null)
            {
                _mesh = ToonMeshUtility.CopyMesh(_importer.BaseMesh);
            }

            _serializedObject.Update();

            if (_importer == null)
            {
                return;
            }

            if (_importer.BaseMesh == null)
            {
                return;
            }

            DrawMesh(_mesh);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(_position)));
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(_rotation)));
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(_scale)));
                }
                EditorGUILayout.EndVertical();
            }
            {
                EditorGUILayout.BeginVertical();
                {
                    if (GUILayout.Button("Clear"))
                    {
                        Clear();
                    }

                    if (GUILayout.Button("Randomize"))
                    {
                        Randomize();
                    }

                    if (GUILayout.Button("Save"))
                    {
                        Save();
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();


            _serializedObject.ApplyModifiedProperties();
        }

        private void Clear()
        {
            for (int index = 0; index < _colors.Length; index++)
            {
                ref Color32 color = ref _colors[index];
                color = White;
            }

            UploadColors();
        }

        private async void Save()
        {
            Assert.IsNotNull(_importer, "_importer != null");

            if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(_importer)) is VertexColorPaintImporter
                importer)
            {
                importer.Colors = _colors.ToArray();
                EditorUtility.SetDirty(importer);
                AssetDatabase.SaveAssetIfDirty(importer);

                await Task.Yield();

                importer.SaveAndReimport();
            }

            // _importer.Colors = _colors.ToArray();
            //
            // EditorUtility.SetDirty(_importer);
            // AssetDatabase.SaveAssetIfDirty(_importer);
            // _importer.SaveAndReimport();
        }

        private void Randomize()
        {
            for (int index = 0; index < _colors.Length; index++)
            {
                static byte GetRandomByte() => (byte) Random.Range(0, byte.MaxValue);
                ref Color32 color = ref _colors[index];
                color = new Color32(
                    GetRandomByte(),
                    GetRandomByte(),
                    GetRandomByte(),
                    byte.MaxValue
                );
            }

            UploadColors();
        }

        private void UploadColors()
        {
            _mesh.SetColors(_colors);
        }

        private static PreviewRenderUtility CreateRenderer() =>
            new()
            {
                camera =
                {
                    clearFlags = CameraClearFlags.SolidColor,
                    backgroundColor = Color.grey,
                    transform =
                    {
                        position = new Vector3(0, 0, -10),
                    },
                },
            };

        private void DrawMesh(Mesh mesh)
        {
            var boundaries = new Rect(0, 0, position.width, position.height);
            _renderer.BeginPreview(boundaries, GUIStyle.none);

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                _renderer.DrawMesh(mesh,
                    Matrix4x4.TRS(_position, Quaternion.Euler(_rotation), _scale),
                    _material, i
                );
            }

            _renderer.camera.Render();


            Texture rt = _renderer.EndPreview();

            EditorGUI.DrawPreviewTexture(new Rect(0, 0, boundaries.width, boundaries.height), rt);
        }


        public static VertexColorPaintEditorWindow Open(VertexColorPaintImporter importer)
        {
            Assert.IsNotNull(importer, "importer != null");
            Assert.IsNotNull(importer.BaseMesh, "importer.BaseMesh != null");

            VertexColorPaintEditorWindow window = CreateInstance<VertexColorPaintEditorWindow>();
            window.titleContent = new GUIContent("Vertex Color Paint Window");
            window.autoRepaintOnSceneChange = true;

            window.Construct(importer);
            window.Show();

            return window;
        }

        private void Construct(VertexColorPaintImporter importer)
        {
            _importer = importer;

            int vertexCount = _mesh.vertexCount;
            _colors = new Color32[vertexCount];

            if (_importer.Colors != null)
            {
                Array.Copy(
                    _importer.Colors, _colors,
                    Mathf.Min(_importer.Colors.Length, vertexCount)
                );
            }
        }
    }
}