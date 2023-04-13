using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Shadows
{
    public class ToonBlobShadows
    {
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
        private readonly DynamicBlobShadowsMesh _dynamicMesh = new();

        private ToonBlobShadowsSettings _blobShadowsSettings;
        private Camera _camera;
        private ScriptableRenderContext _context;
        private Material _material;
        private ToonShadowSettings _settings;

        private void EnsureAssetsAreCreated()
        {
            if (_material == null)
            {
                var shader = Shader.Find("Hidden/Toon RP/Blob Shadow Pass");
                _material = new Material(shader)
                {
                    name = "Toon RP Blob Shadow Pass",
                };
            }
        }

        public void Setup(in ScriptableRenderContext context, in ToonShadowSettings settings, Camera camera)
        {
            _camera = camera;
            _context = context;
            _settings = settings;
            _blobShadowsSettings = settings.Blobs;

            int atlasSize = (int) _blobShadowsSettings.AtlasSize;
            _cmd.GetTemporaryRT(ShadowMapId, atlasSize, atlasSize, 0, FilterMode.Bilinear, RenderTextureFormat.R8,
                RenderTextureReadWrite.Linear
            );
            ExecuteBuffer();
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

            Mesh mesh = _dynamicMesh.Construct(_culling.Renderers, _culling.Bounds);
            _cmd.DrawMesh(mesh, Matrix4x4.identity, _material, SubmeshIndex, ShaderPass);
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