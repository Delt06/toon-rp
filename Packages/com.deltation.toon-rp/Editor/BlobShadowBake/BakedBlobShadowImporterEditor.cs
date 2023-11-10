using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine.UIElements;

namespace DELTation.ToonRP.Editor.BlobShadowBake
{
    [CustomEditor(typeof(BakedBlobShadowImporter))]
    public class BakedBlobShadowImporterEditor : ScriptedImporterEditor
    {
        [MenuItem("Assets/Create/Toon RP/Baked Blob Shadow")]
        public static void CreateNewAsset()
        {
            ProjectWindowUtil.CreateAssetWithContent(
                "New Baked Blob Shadow." + BakedBlobShadowImporter.Extension, string.Empty
            );
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            UiElementsUtils.AddAllFields(serializedObject, root);
            root.Add(new IMGUIContainer(ApplyRevertGUI));
            return root;
        }
    }
}