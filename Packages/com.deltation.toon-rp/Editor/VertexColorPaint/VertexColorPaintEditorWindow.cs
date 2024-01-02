using System;
using System.Collections.Generic;
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
        private const float DesiredSize = 1.0f;
        private const int ControlsPadding = 8;
        private static readonly Color32 White = new(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
        [SerializeField] private VertexColorPaintImporter _importer;
        [SerializeField] private Mesh _mesh;
        [SerializeField] private Color32[] _colors;
        [SerializeField] [Range(0.02f, 0.5f)]
        private float _brushSize = 0.1f;
        [SerializeField] [Range(0, 1)]
        private float _brushHardness = 1.0f;
        [SerializeField] private bool _viewAlpha;
        [SerializeField] private Color32 _brushColor = Color.black;
        [SerializeField] private Vector3 _cameraPosition;
        [SerializeField] private float _cameraYaw;
        [SerializeField] private float _cameraPitch;

        private readonly HashSet<KeyCode> _heldKeys = new();

        private readonly ToonPipelineMaterial _material = new("Hidden/Vertex Color Paint", "Vertex Color Paint");
        private bool _autoAdjustCamera;
        private Rect _controlsRect;
        private PreviewRenderUtility _renderer;
        private Rect _renderRect;
        private SerializedObject _serializedObject;
        private double _time;
        private Vector3[] _vertices;

        private void OnEnable()
        {
            _heldKeys.Clear();
            _renderer ??= CreateRenderer();
            _serializedObject = new SerializedObject(this);

            UploadCameraTransform();

            _time = EditorApplication.timeSinceStartup;
        }

        private void OnDisable()
        {
            _material.Dispose();
            _renderer?.Cleanup();
        }

        private void OnGUI()
        {
            if (_importer == null || _importer.BaseMesh == null)
            {
                return;
            }

            if (_mesh == null)
            {
                _mesh = ToonMeshUtility.CopyMesh(_importer.BaseMesh);
                UploadColors();
                UpdateVertices();
            }

            if (_vertices == null)
            {
                UpdateVertices();
            }

            _serializedObject.Update();

            UpdateRects();
            HandleInput();

            DrawMesh(_mesh);
            DrawControls();

            _serializedObject.ApplyModifiedProperties();
        }

        private static Vector3 GetModelScale(Bounds bounds) => GetModelScaleScalar(bounds) * Vector3.one;

        private static float GetModelScaleScalar(Bounds bounds)
        {
            float x = DesiredSize / bounds.size.x;
            float y = DesiredSize / bounds.size.y;
            float z = DesiredSize / bounds.size.z;
            float scale = Mathf.Min(x, Mathf.Min(y, z));
            return scale;
        }

        private void AutoAdjustCamera()
        {
            Mesh baseMesh = _importer.BaseMesh;
            if (baseMesh == null)
            {
                return;
            }

            _cameraYaw = 0.0f;
            _cameraPitch = 0.0f;
            Bounds bounds = baseMesh.bounds;

            static float MaxComponent(Vector3 vector) => Mathf.Max(vector.x, Mathf.Max(vector.y, vector.z));
            float modelScale = GetModelScaleScalar(bounds);
            _cameraPosition = bounds.center * modelScale +
                              MaxComponent(bounds.size) * modelScale * 4 * Vector3.back;
        }

        private void UpdateRects()
        {
            const float controlsHeight = 100;
            _controlsRect = new Rect(ControlsPadding, position.height - controlsHeight - ControlsPadding,
                position.width - ControlsPadding * 2, controlsHeight
            );
            _renderRect = new Rect(0, 0, position.width, _controlsRect.y);
        }

        private void DrawControls()
        {
            GUILayout.BeginArea(_controlsRect);
            EditorGUILayout.Space(ControlsPadding);
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(_brushSize)));
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(_brushHardness)));
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(_brushColor)));
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty(nameof(_viewAlpha)));
                }
                EditorGUILayout.EndVertical();
            }
            {
                EditorGUILayout.BeginVertical();
                {
                    if (GUILayout.Button("Reset Camera"))
                    {
                        _autoAdjustCamera = true;
                    }

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("←"))
                    {
                        RotateCamera(0, -90);
                    }

                    if (GUILayout.Button("→"))
                    {
                        RotateCamera(0, 90);
                    }

                    if (GUILayout.Button("↑"))
                    {
                        RotateCamera(-90, 0);
                    }

                    if (GUILayout.Button("↓"))
                    {
                        RotateCamera(90, 0);
                    }

                    EditorGUILayout.EndHorizontal();
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
            GUILayout.EndArea();
        }

        private void UpdateVertices()
        {
            _vertices = _mesh.vertices;
        }

        private void HandleInput()
        {
            double oldTime = _time;
            _time = EditorApplication.timeSinceStartup;
            float deltaTime = (float) (_time - oldTime);
            Event e = Event.current;
            const int leftMouseButton = 0;
            const int rightMouseButton = 1;
            Transform cameraTransform = _renderer.camera.transform;
            bool cameraTransformDirty = false;

            if (_autoAdjustCamera)
            {
                AutoAdjustCamera();
                _autoAdjustCamera = false;
                cameraTransformDirty = true;
            }

            bool insideRect = _renderRect.Contains(e.mousePosition);
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (e.type)
            {
                case EventType.MouseDrag when e.button == leftMouseButton && insideRect:
                case EventType.MouseDown when e.button == leftMouseButton && insideRect:
                {
                    Vector3 viewport = GuiPositionToRenderViewport(e.mousePosition);
                    Ray ray = _renderer.camera.ViewportPointToRay(viewport);
                    Matrix4x4 meshMatrix = MeshMatrix();
                    if (ToonMeshUtility.IntersectRayMesh(ray, _mesh, meshMatrix, out RaycastHit hit))
                    {
                        Paint(hit.point, meshMatrix);
                        UploadColors();
                        e.Use();
                    }

                    break;
                }
                case EventType.MouseDrag when e.button == rightMouseButton && insideRect:
                {
                    Vector2 delta = e.delta * 0.2f;
                    _cameraYaw += delta.x;
                    _cameraPitch -= delta.y;
                    cameraTransformDirty = true;
                    break;
                }
                case EventType.MouseDown:
                {
                    _heldKeys.Add(KeyCode.Mouse0 + e.button);
                    break;
                }
                case EventType.MouseUp:
                {
                    _heldKeys.Remove(KeyCode.Mouse0 + e.button);
                    break;
                }
                case EventType.KeyDown:
                {
                    _heldKeys.Add(e.keyCode);
                    break;
                }
                case EventType.KeyUp:
                {
                    _heldKeys.Remove(e.keyCode);
                    break;
                }
            }

            if (_heldKeys.Contains(KeyCode.Mouse1))
            {
                foreach (KeyCode heldKey in _heldKeys)
                {
                    Vector3 movementDirection = Vector3.zero;
                    // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                    switch (heldKey)
                    {
                        case KeyCode.W:
                        {
                            movementDirection += cameraTransform.forward;
                            break;
                        }
                        case KeyCode.S:
                        {
                            movementDirection -= cameraTransform.forward;
                            break;
                        }

                        case KeyCode.D:
                        {
                            movementDirection += cameraTransform.right;
                            break;
                        }
                        case KeyCode.A:
                        {
                            movementDirection -= cameraTransform.right;
                            break;
                        }

                        case KeyCode.E:
                        {
                            movementDirection += cameraTransform.up;
                            break;
                        }
                        case KeyCode.Q:
                        {
                            movementDirection -= cameraTransform.up;
                            break;
                        }
                    }

                    movementDirection = Vector3.ClampMagnitude(movementDirection, 1.0f);
                    if (movementDirection.sqrMagnitude > 0.0001f)
                    {
                        _cameraPosition += deltaTime * 2f * movementDirection;
                        cameraTransformDirty = true;
                    }
                }
            }

            if (cameraTransformDirty)
            {
                UploadCameraTransform();
            }
        }

        private void UploadCameraTransform()
        {
            Quaternion rotation = ComputeCameraRotation();
            _renderer.camera.transform.SetPositionAndRotation(_cameraPosition, rotation);
            _serializedObject.Update();
            Repaint();
        }

        private Quaternion ComputeCameraRotation() =>
            Quaternion.Euler(_cameraPitch, 0, 0) *
            Quaternion.Euler(0, _cameraYaw, 0);

        private void RotateCamera(float deltaPitch, float deltaYaw)
        {
            _cameraPitch += deltaPitch;
            _cameraYaw += deltaYaw;
            Bounds bounds = _mesh.bounds;
            Vector3 center = bounds.center * GetModelScaleScalar(bounds);
            float distance = Vector3.Distance(_cameraPosition, center);
            _cameraPosition = center + ComputeCameraRotation() * Vector3.back * distance;
            UploadCameraTransform();
        }

        private Vector3 GuiPositionToRenderViewport(Vector2 guiPosition) =>
            new(
                Mathf.InverseLerp(_renderRect.xMin, _renderRect.xMax, guiPosition.x),
                1 - Mathf.InverseLerp(_renderRect.yMin, _renderRect.yMax, guiPosition.y),
                0
            );

        private void Paint(Vector3 hit, Matrix4x4 matrix)
        {
            float worldSpaceBrushSize =
                (_renderer.camera.cameraToWorldMatrix * new Vector4(_brushSize, 0, 0, 0)).magnitude;
            hit = matrix.inverse * new Vector4(hit.x, hit.y, hit.z, 1);
            float sqrRadius = worldSpaceBrushSize * worldSpaceBrushSize / GetModelScale(_mesh.bounds).sqrMagnitude;

            for (int index = 0; index < _vertices.Length; index++)
            {
                if ((hit - _vertices[index]).sqrMagnitude < sqrRadius)
                {
                    _colors[index] = Color32.Lerp(_colors[index], _brushColor, _brushHardness);
                }
            }
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
                    farClipPlane = DesiredSize * 10,
                    nearClipPlane = DesiredSize * 0.1f,
                },
            };

        private void DrawMesh(Mesh mesh)
        {
            Material material = _material.GetOrCreate();
            _renderer.BeginPreview(new Rect(0, 0, _renderRect.width, _renderRect.height), GUIStyle.none);

            Matrix4x4 meshMatrix = MeshMatrix();
            material.SetKeyword("VIEW_ALPHA", _viewAlpha);

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                _renderer.DrawMesh(mesh,
                    meshMatrix,
                    material, i
                );
            }

            _renderer.camera.Render();

            Texture rt = _renderer.EndPreview();
            EditorGUI.DrawPreviewTexture(_renderRect, rt);
        }

        private Matrix4x4 MeshMatrix() => Matrix4x4.Scale(GetModelScale(_mesh.bounds));

        public static void Open(VertexColorPaintImporter importer)
        {
            Assert.IsNotNull(importer, "importer != null");
            Assert.IsNotNull(importer.BaseMesh, "importer.BaseMesh != null");

            VertexColorPaintEditorWindow window = CreateInstance<VertexColorPaintEditorWindow>();
            window.titleContent = new GUIContent("Vertex Color Paint Window");
            window.autoRepaintOnSceneChange = true;

            window.Construct(importer);
            window.Show();
        }

        private void Construct(VertexColorPaintImporter importer)
        {
            _importer = importer;

            int vertexCount = _importer.BaseMesh.vertexCount;
            _colors = new Color32[vertexCount];

            if (_importer.Colors != null)
            {
                Array.Copy(
                    _importer.Colors, _colors,
                    Mathf.Min(_importer.Colors.Length, vertexCount)
                );
            }

            _autoAdjustCamera = true;
        }
    }
}