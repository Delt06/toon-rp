using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityBlendMode = UnityEngine.Rendering.BlendMode;

namespace DELTation.ToonRP.Shadows
{
    public class ToonBlobShadows
    {
        public const string ShaderName = "Hidden/Toon RP/Blob Shadow Pass";
        private static readonly int ShadowMapId = Shader.PropertyToID("_ToonRP_BlobShadowMap");
        private static readonly int MinSizeId = Shader.PropertyToID("_ToonRP_BlobShadows_Min_Size");
        private static readonly int CoordsOffsetId = Shader.PropertyToID("_ToonRP_BlobShadowCoordsOffset");
        private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int BlendOpId = Shader.PropertyToID("_BlendOp");
        private static readonly int BakedBlobShadowTextureId = Shader.PropertyToID("_BakedBlobShadowTexture");
        private readonly ToonBlobShadowsCulling _culling = new();

        private readonly DynamicBlobShadowsMesh[] _shadowMeshes;

        private ToonBlobShadowsSettings _blobShadowsSettings;
        private Camera _camera;
        private ScriptableRenderContext _context;
        private Material _material;
        private ToonShadowSettings _settings;

        public ToonBlobShadows()
        {
            _shadowMeshes = new DynamicBlobShadowsMesh[BlobShadowTypes.Count];
            _shadowMeshes[(int) BlobShadowType.Circle] = new DynamicCircleBlobShadowsMesh();
            _shadowMeshes[(int) BlobShadowType.Square] = new DynamicSquareBlobShadowsMesh();
            _shadowMeshes[(int) BlobShadowType.Baked] = new DynamicBakedBlobShadowsMesh();
        }

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
            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.GetTemporaryRT(ShadowMapId, atlasSize, atlasSize, 0, FilterMode.Bilinear, RenderTextureFormat.R8,
                RenderTextureReadWrite.Linear
            );
            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Render()
        {
            EnsureAssetsAreCreated();

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.BlobShadows)))
            {
                cmd.SetRenderTarget(ShadowMapId,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                );
                cmd.ClearRenderTarget(false, true, Color.black);

                float maxDistance = Mathf.Min(_settings.MaxDistance, _camera.farClipPlane);
                _culling.Cull(BlobShadowsManager.Renderers, _camera, maxDistance);
                DrawShadows(cmd);

                {
                    Vector2 min = _culling.Bounds.Min;
                    Vector2 size = _culling.Bounds.Size;
                    var minSize = new Vector4(
                        min.x, min.y,
                        size.x, size.y
                    );
                    cmd.SetGlobalVector(MinSizeId, minSize);
                }

                cmd.SetGlobalVector(CoordsOffsetId, _settings.Blobs.ShadowPositionOffset);
            }

            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void DrawShadows(CommandBuffer cmd)
        {
            _material.SetFloat(SaturationId, _blobShadowsSettings.Saturation);
            SetupBlending();

            for (int shadowType = 0; shadowType < BlobShadowTypes.Count; shadowType++)
            {
                DynamicBlobShadowsMesh dynamicShadowMesh = _shadowMeshes[shadowType];
                Mesh mesh = dynamicShadowMesh.Construct(_culling.Renderers, _culling.Bounds);
                if (mesh != null)
                {
                    // Each batch corresponds to a submesh.
                    // Renderers are batched based on their baked shadow textures.
                    for (int batchIndex = 0; batchIndex < dynamicShadowMesh.Batches.Count; batchIndex++)
                    {
                        Texture2D bakedShadowTexture = dynamicShadowMesh.Batches[batchIndex].BakedShadowTexture;

                        if (bakedShadowTexture)
                        {
                            cmd.SetGlobalTexture(BakedBlobShadowTextureId, bakedShadowTexture);
                        }

                        cmd.DrawMesh(mesh, Matrix4x4.identity, _material, batchIndex, shadowType);
                    }
                }
            }
        }

        private void SetupBlending()
        {
            (UnityBlendMode srcBlend, UnityBlendMode dstBlend, BlendOp blendOp) = _blobShadowsSettings.Mode switch
            {
                BlobShadowsMode.MetaBalls => (UnityBlendMode.SrcColor, UnityBlendMode.One, BlendOp.Add),
                BlobShadowsMode.Default => (UnityBlendMode.One, UnityBlendMode.One, BlendOp.Max),
                _ => throw new ArgumentOutOfRangeException(),
            };
            _material.SetFloat(SrcBlendId, (float) srcBlend);
            _material.SetFloat(DstBlendId, (float) dstBlend);
            _material.SetFloat(BlendOpId, (float) blendOp);
        }

        public void Cleanup()
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.ReleaseTemporaryRT(ShadowMapId);
            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}