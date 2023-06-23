using UnityEditor;

namespace DELTation.ToonRP.Editor.VertexColorPaint
{
    public static class VertexColorPaintEditorUtility
    {
        [MenuItem("Assets/Create/Toon RP/Vertex Color Paint")]
        public static void CreateNewAsset()
        {
            ProjectWindowUtil.CreateAssetWithContent(
                "New Vertex Color Paint." + VertexColorPaintImporter.Extension, string.Empty
            );
        }
    }
}