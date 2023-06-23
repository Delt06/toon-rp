using Unity.Collections;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Editor.VertexColorPaint
{
    [ScriptedImporter(1, Extension)]
    public class VertexColorPaintImporter : ScriptedImporter
    {
        public const string Extension = "vertexcolorpaint";

        [SerializeField] private Mesh _baseMesh;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            if (_baseMesh == null)
            {
                return;
            }

            Mesh mesh = CopyMesh(_baseMesh);
            SetColors(mesh);

            bool markNoLongerReadable = !_baseMesh.isReadable;
            mesh.UploadMeshData(markNoLongerReadable);

            ctx.AddObjectToAsset("mesh", mesh);
            ctx.SetMainObject(mesh);
        }

        // Adapted from https://github.com/GeorgeAdamon/FastMeshCopy
        private static Mesh CopyMesh(Mesh source)
        {
            var outMesh = new Mesh
            {
                name = source.name,
                bounds = source.bounds,
            };

            using Mesh.MeshDataArray readArray = Mesh.AcquireReadOnlyMeshData(source);
            //-------------------------------------------------------------
            // INPUT INFO
            //-------------------------------------------------------------
            Mesh.MeshData readData = readArray[0];

            // Formats
            VertexAttributeDescriptor[] vertexFormat = source.GetVertexAttributes();
            IndexFormat indexFormat = source.indexFormat;
            bool isIndexShort = indexFormat == IndexFormat.UInt16;

            // Counts
            int vertexCount = readData.vertexCount;
            int indexCount =
                isIndexShort ? readData.GetIndexData<ushort>().Length : readData.GetIndexData<uint>().Length;


            //-------------------------------------------------------------
            // OUTPUT SETUP
            //-------------------------------------------------------------
            Mesh.MeshDataArray writeArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData writeData = writeArray[0];
            writeData.SetVertexBufferParams(vertexCount, vertexFormat);
            writeData.SetIndexBufferParams(indexCount, indexFormat);

            //-------------------------------------------------------------
            // MEMORY COPYING
            //-------------------------------------------------------------
            NativeArray<byte> inData;
            NativeArray<byte> outData;

            // Vertices
            inData = readData.GetVertexData<byte>();
            outData = writeData.GetVertexData<byte>();
            inData.CopyTo(outData);


            // Indices
            inData = readData.GetIndexData<byte>();
            outData = writeData.GetIndexData<byte>();

            inData.CopyTo(outData);

            //-------------------------------------------------------------
            // FINALIZATION
            //-------------------------------------------------------------
            writeData.subMeshCount = source.subMeshCount;

            // Set all sub-meshes
            for (int i = 0; i < source.subMeshCount; i++)
            {
                writeData.SetSubMesh(i,
                    new SubMeshDescriptor((int) source.GetIndexStart(i),
                        (int) source.GetIndexCount(i)
                    )
                );
            }

            Mesh.ApplyAndDisposeWritableMeshData(writeArray, outMesh);
            return outMesh;
        }

        private static void SetColors(Mesh mesh)
        {
            var colors = new NativeArray<Color32>(mesh.vertexCount, Allocator.Temp);

            int vertexCount = mesh.vertexCount;
            for (int i = 0; i < vertexCount; i++)
            {
                static byte RandomByte() => (byte) Random.Range(0, byte.MaxValue);
                colors[i] = new Color32(RandomByte(), RandomByte(), RandomByte(), RandomByte());
            }

            mesh.SetColors(colors);
            colors.Dispose();
        }
    }
}