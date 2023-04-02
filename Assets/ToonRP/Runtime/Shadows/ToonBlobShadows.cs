using UnityEngine;
using UnityEngine.Rendering;

namespace ToonRP.Runtime.Shadows
{
    public class ToonBlobShadows
    {
        private const int MaxMatrices = 1023;
        private static readonly int ShadowMapId = Shader.PropertyToID("_ToonRP_BlobShadowMap");
        private static readonly int MinSizeId = Shader.PropertyToID("_ToonRP_BlobShadows_Min_Size");
        private readonly CommandBuffer _cmd = new() { name = "Blob Shadows" };

        private readonly Vector3 _worldMax = new(10, 0, 10);
        private readonly Vector3 _worldMin = new(-10, 0, -10);
        private ScriptableRenderContext _context;
        private Material _material;
        private Matrix4x4[] _matrices;
        private int _matricesCount;
        private Mesh _mesh;

        private void EnsureAssetsAreCreated()
        {
            if (_material == null)
            {
                var shader = Shader.Find("Hidden/Toon RP/Blob Shadow Pass");
                _material = new Material(shader)
                {
                    name = "Toon RP Blob Shadow Pass",
                    enableInstancing = true,
                };
            }

            if (_mesh == null)
            {
                _mesh = new Mesh();
                _mesh.SetVertices(new Vector3[]
                    {
                        new(-0.5f, -0.5f),
                        new(0.5f, -0.5f),
                        new(0.5f, 0.5f),
                        new(-0.5f, 0.5f),
                    }
                );
                _mesh.SetIndices(new[] { 0, 1, 2, 2, 3, 0 }, MeshTopology.Triangles, 0);
            }
        }

        public void Setup(ScriptableRenderContext context)
        {
            _context = context;
            _cmd.GetTemporaryRT(ShadowMapId, 1024, 1024, 0, FilterMode.Bilinear, RenderTextureFormat.R8);
            ExecuteBuffer();

            if (SystemInfo.supportsInstancing)
            {
                _matrices ??= new Matrix4x4[MaxMatrices];
            }
        }

        public void Render()
        {
            EnsureAssetsAreCreated();

            _cmd.SetRenderTarget(ShadowMapId,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            _cmd.ClearRenderTarget(false, true, Color.black);

            if (SystemInfo.supportsInstancing)
            {
                _matricesCount = 0;

                Vector3 inverseWorldSize = _worldMax - _worldMin;
                inverseWorldSize.x = 1.0f / inverseWorldSize.x;
                inverseWorldSize.y = 1.0f / inverseWorldSize.z;
                inverseWorldSize.z = 1.0f;
                Quaternion rotation = Quaternion.identity;

                foreach (BlobShadowRenderer renderer in BlobShadowsManager.Renderers)
                {
                    Vector3 position = WorldToHClip(renderer.transform.position);
                    // the quad's size is one by one
                    float diameter = renderer.Radius * 2.0f;
                    Vector3 scale = inverseWorldSize * diameter * 2;

                    // _cmd.DrawMesh(_mesh, Matrix4x4.TRS(position, rotation, scale), _material, 0, 0);

                    _matrices[_matricesCount] = Matrix4x4.TRS(position, rotation, scale);
                    _matricesCount++;

                    if (_matricesCount < MaxMatrices)
                    {
                        continue;
                    }

                    _cmd.DrawMeshInstanced(_mesh, 0, _material, 0, _matrices, _matricesCount);
                    _matricesCount = 0;
                }

                if (_matricesCount > 0)
                {
                    _cmd.DrawMeshInstanced(_mesh, 0, _material, 0, _matrices, _matricesCount);
                }
            }

            // TODO: implement drawing without instancing
            // else{
            // foreach ()
            // {
            //     _cmd.DrawMesh();
            // }}

            {
                Vector3 size = _worldMax - _worldMin;
                var minSize = new Vector4(
                    _worldMin.x, _worldMin.z,
                    size.x, size.z
                );
                _cmd.SetGlobalVector(MinSizeId, minSize);
            }


            ExecuteBuffer();
        }

        private Vector3 WorldToHClip(Vector3 position)
        {
            float x = Mathf.InverseLerp(_worldMin.x, _worldMax.x, position.x);
            x = (x - 0.5f) * 2.0f;
            float y = Mathf.InverseLerp(_worldMin.z, _worldMax.z, position.z);
            y = (y - 0.5f) * 2.0f;

            if (SystemInfo.graphicsUVStartsAtTop)
            {
                y *= -1.0f;
            }

            return new Vector3(x, y, 0.5f);
        }

        public void Cleanup()
        {
            _cmd.ReleaseTemporaryRT(ShadowMapId);
            ExecuteBuffer();
        }

        private void ExecuteBuffer()
        {
            _context.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }
    }
}