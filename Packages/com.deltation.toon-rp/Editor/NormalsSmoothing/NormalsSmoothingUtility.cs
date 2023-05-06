// Adapted from https://github.com/Delt06/urp-toon-shader/blob/master/Packages/com.deltation.toon-shader/Assets/DELTation/ToonShader/Editor/NormalsSmoothing/NormalsSmoothingUtility.cs

using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DELTation.ToonRP.Editor.NormalsSmoothing
{
    public class NormalsSmoothingUtility : EditorWindow
    {
        public const int UvChannel = 2;
        public const float MaxSmoothingAngle = 180f;

        [SerializeField]
        private Mesh _sourceMesh;

        [SerializeField]
        private float _smoothingAngle = MaxSmoothingAngle;

        [SerializeField]
        private Channel _channel = Channel.UV2;

        private void OnGUI()
        {
            _sourceMesh = (Mesh) EditorGUILayout.ObjectField("Source Mesh", _sourceMesh, typeof(Mesh), false);
            _smoothingAngle = EditorGUILayout.Slider("Smoothing Angle", _smoothingAngle, 0, MaxSmoothingAngle);
            _channel = (Channel) EditorGUILayout.EnumPopup("Channel", _channel);

            EditorGUILayout.HelpBox(
                "Skinned should always use the Tangents channel for correct skinning.\nThis, however, makes using normal maps impossible.",
                MessageType.Info
            );

            if (_sourceMesh == null)
            {
                EditorGUILayout.HelpBox("No mesh selected", MessageType.Error);
                return;
            }

            if (!_sourceMesh.isReadable)
            {
                EditorGUILayout.HelpBox("Enable Read/Write in model import settings.", MessageType.Error);
                return;
            }


            if (_channel == Channel.UV2)
            {
                var uvs = new List<Vector4>();
                _sourceMesh.GetUVs(UvChannel, uvs);
                if (uvs.Count > 0)
                {
                    EditorGUILayout.HelpBox($"UV{UvChannel} is busy, it will be overwritten.", MessageType.Warning);
                }

                var boneWeights = new List<BoneWeight>();
                _sourceMesh.GetBoneWeights(boneWeights);

                if (boneWeights.Count > 0)
                {
                    EditorGUILayout.HelpBox(
                        "The mesh seems to be a skinned mesh. Change the Channel to Tangents for correct behavior.",
                        MessageType.Warning
                    );
                }
            }

            if (GUILayout.Button("Compute Smoothed Normals"))
            {
                ComputeSmoothedNormals();
            }
        }

        [MenuItem("Window/Toon RP/Normals Smoothing Utility")]
        private static void OpenWindow()
        {
            NormalsSmoothingUtility window = CreateWindow<NormalsSmoothingUtility>();
            window.titleContent = new GUIContent("Normals Smoothing Utility");
            window.ShowUtility();
        }

        private void ComputeSmoothedNormals()
        {
            Close();

            Assert.IsNotNull(_sourceMesh);
            Assert.IsTrue(_smoothingAngle > 0f);

            Mesh smoothedMesh = Instantiate(_sourceMesh);
            smoothedMesh.name = _sourceMesh.name + "_SmoothedNormals";
            smoothedMesh.CalculateNormalsAndWriteToChannel(_smoothingAngle, _channel == Channel.UV2 ? UvChannel : null);
            CreateMeshAsset(smoothedMesh);
        }

        private static void CreateMeshAsset(Mesh mesh)
        {
            string path = EditorUtility.SaveFilePanelInProject("Save mesh", mesh.name, "asset",
                "Select mesh asset path"
            );
            if (string.IsNullOrEmpty(path))
            {
                DestroyImmediate(mesh);
                return;
            }

            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();

            Selection.activeObject = mesh;
        }

        private enum Channel
        {
            UV2,
            Tangents,
        }
    }
}