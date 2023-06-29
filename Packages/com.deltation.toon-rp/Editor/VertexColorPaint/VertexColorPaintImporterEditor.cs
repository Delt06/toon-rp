using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace DELTation.ToonRP.Editor.VertexColorPaint
{
    [CustomEditor(typeof(VertexColorPaintImporter))]
    public class VertexColorPaintImporterEditor : ScriptedImporterEditor
    {
        private SerializedProperty _baseMesh;

        public override void OnEnable()
        {
            base.OnEnable();
            _baseMesh = serializedObject.FindProperty(nameof(VertexColorPaintImporter.BaseMesh));
        }

        [MenuItem("Assets/Create/Toon RP/Vertex Color Paint")]
        public static void CreateNewAsset()
        {
            ProjectWindowUtil.CreateAssetWithContent(
                "New Vertex Color Paint." + VertexColorPaintImporter.Extension, string.Empty
            );
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_baseMesh);

            if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox("Can't open the Editor in multi-editing mode.", MessageType.Warning);
            }
            else if (GUILayout.Button("Open Editor"))
            {
                VertexColorPaintEditorWindow.Open((VertexColorPaintImporter) target);
            }

            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }
    }
}