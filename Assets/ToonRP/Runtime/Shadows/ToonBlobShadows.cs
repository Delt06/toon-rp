using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace ToonRP.Runtime.Shadows
{
    public class ToonBlobShadows
    {
        private const int MaxMatrices = 1023;
        private const int SubmeshIndex = 0;
        private const int ShaderPass = 0;
        private static readonly int ShadowMapId = Shader.PropertyToID("_ToonRP_BlobShadowMap");
        private static readonly int MinSizeId = Shader.PropertyToID("_ToonRP_BlobShadows_Min_Size");
        private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int BlendOpId = Shader.PropertyToID("_BlendOp");
        private readonly CommandBuffer _cmd = new() { name = "Blob Shadows" };
        private readonly ToonBlobShadowsCulling _culling = new();

        private ToonBlobShadowsSettings _blobShadowsSettings;
        private Camera _camera;
        private ScriptableRenderContext _context;
        private Vector3 _inverseWorldSize;
        private Material _material;
        private Matrix4x4[] _matrices;
        private int _matricesCount;
        private Mesh _mesh;
        private ToonShadowSettings _settings;
        private bool _useInstancing;

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

        public void Setup(in ScriptableRenderContext context, in ToonShadowSettings settings, Camera camera)
        {
            _camera = camera;
            _context = context;
            _settings = settings;
            _blobShadowsSettings = settings.Blobs;

            int atlasSize = (int) _blobShadowsSettings.AtlasSize;
            _cmd.GetTemporaryRT(ShadowMapId, atlasSize, atlasSize, 0, FilterMode.Bilinear, RenderTextureFormat.R8);
            ExecuteBuffer();

            _useInstancing = SystemInfo.supportsInstancing && _blobShadowsSettings.GPUInstancing;

            if (_useInstancing)
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

            float maxDistance = Mathf.Min(_settings.MaxDistance, _camera.farClipPlane);
            _culling.Cull(BlobShadowsManager.Renderers, _camera, maxDistance);
            DrawShadows();

            {
                Vector2 min = _culling.Bounds.Min;
                Vector2 size = _culling.Bounds.Size;
                var minSize = new Vector4(
                    min.x, min.y,
                    size.x, size.y
                );
                _cmd.SetGlobalVector(MinSizeId, minSize);
            }


            ExecuteBuffer();
        }

        private void DrawShadows()
        {
            _material.SetFloat(SaturationId, _blobShadowsSettings.Saturation);
            SetupBlending();

            _inverseWorldSize = _culling.Bounds.Size;
            _inverseWorldSize.x = 1.0f / _inverseWorldSize.x;
            _inverseWorldSize.y = 1.0f / _inverseWorldSize.y;
            _inverseWorldSize.z = 1.0f;

            if (_useInstancing)
            {
                _matricesCount = 0;

                foreach (ToonBlobShadowsCulling.RendererData renderer in _culling.Renderers)
                {
                    _matrices[_matricesCount] = ComputeMatrix(renderer);
                    _matricesCount++;

                    if (_matricesCount < MaxMatrices)
                    {
                        continue;
                    }

                    _cmd.DrawMeshInstanced(_mesh, SubmeshIndex, _material, ShaderPass, _matrices, _matricesCount);
                    _matricesCount = 0;
                }

                if (_matricesCount > 0)
                {
                    _cmd.DrawMeshInstanced(_mesh, SubmeshIndex, _material, ShaderPass, _matrices, _matricesCount);
                }
            }
            else
            {
                foreach (ToonBlobShadowsCulling.RendererData renderer in _culling.Renderers)
                {
                    _cmd.DrawMesh(_mesh, ComputeMatrix(renderer), _material, SubmeshIndex, ShaderPass);
                }
            }
        }

        private void SetupBlending()
        {
            (BlendMode srcBlend, BlendMode dstBlend, BlendOp blendOp) = _blobShadowsSettings.Mode switch
            {
                BlobShadowsMode.MetaBalls => (BlendMode.SrcColor, BlendMode.One, BlendOp.Add),
                BlobShadowsMode.Default => (BlendMode.One, BlendMode.One, BlendOp.Max),
                _ => throw new ArgumentOutOfRangeException(),
            };
            _material.SetFloat(SrcBlendId, (float) srcBlend);
            _material.SetFloat(DstBlendId, (float) dstBlend);
            _material.SetFloat(BlendOpId, (float) blendOp);
        }

        private Matrix4x4 ComputeMatrix(in ToonBlobShadowsCulling.RendererData renderer)
        {
            Quaternion rotation = Quaternion.identity;

            Vector3 position = WorldToHClip(renderer.Position);
            // the quad's size is one by one
            float diameter = renderer.Radius * 2.0f;
            Vector3 scale = _inverseWorldSize * diameter * 2;

            return Matrix4x4.TRS(position, rotation, scale);
        }

        private Vector3 WorldToHClip(Vector3 position)
        {
            Vector2 boundsMin = _culling.Bounds.Min;
            Vector2 boundsMax = _culling.Bounds.Max;
            float x = Mathf.InverseLerp(boundsMin.x, boundsMax.x, position.x);
            x = (x - 0.5f) * 2.0f;
            float y = Mathf.InverseLerp(boundsMin.y, boundsMax.y, position.z);
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