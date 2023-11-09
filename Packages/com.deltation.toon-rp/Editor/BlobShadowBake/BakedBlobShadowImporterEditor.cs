using UnityEditor;
using UnityEditor.AssetImporters;

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
    }
}