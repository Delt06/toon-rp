using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine.UIElements;

namespace DELTation.ToonRP.Editor.BlobShadowBake
{
    [CustomEditor(typeof(BakedBlobShadowsAtlasImporter))]
    public class BakedBlobShadowsAtlasImporterEditor : ScriptedImporterEditor
    {
        [MenuItem("Assets/Create/Toon RP/Baked Blob Shadows Atlas")]
        public static void CreateNewAsset()
        {
            ProjectWindowUtil.CreateAssetWithContent(
                "New Baked Blob Shadows Atlas." + BakedBlobShadowsAtlasImporter.Extension, string.Empty
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