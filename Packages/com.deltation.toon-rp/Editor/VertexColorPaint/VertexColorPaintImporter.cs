using UnityEditor.AssetImporters;
using UnityEngine;

namespace DELTation.ToonRP.Editor.VertexColorPaint
{
    [ScriptedImporter(1, Extension)]
    public class VertexColorPaintImporter : ScriptedImporter
    {
        public const string Extension = "vertexcolorpaint";

        public Mesh BaseMesh;
        public Color32[] Colors;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            if (BaseMesh == null)
            {
                return;
            }

            Mesh mesh = ToonMeshUtility.CopyMesh(BaseMesh);
            SetColors(mesh, Colors);

            bool markNoLongerReadable = !BaseMesh.isReadable;
            mesh.UploadMeshData(markNoLongerReadable);

            ctx.AddObjectToAsset("mesh", mesh);
            ctx.SetMainObject(mesh);
        }

        private static void SetColors(Mesh mesh, Color32[] colors)
        {
            if (colors == null)
            {
                return;
            }

            if (colors.Length == mesh.vertexCount)
            {
                mesh.SetColors(colors);
            }
            else
            {
                Debug.LogWarning($"colors.Length ({colors.Length}) != mesh.vertexCount ({mesh.vertexCount})");
            }
        }
    }
}