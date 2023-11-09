using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityBlendMode = UnityEngine.Rendering.BlendMode;

namespace DELTation.ToonRP.Shadows
{
    public class ToonBlobShadows
    {
        private const int SubmeshIndex = 0;

        public const string ShaderName = "Hidden/Toon RP/Blob Shadow Pass";
        private static readonly int ShadowMapId = Shader.PropertyToID("_ToonRP_BlobShadowMap");
        private static readonly int MinSizeId = Shader.PropertyToID("_ToonRP_BlobShadows_Min_Size");
        private static readonly int CoordsOffsetId = Shader.PropertyToID("_ToonRP_BlobShadowCoordsOffset");
        private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int BlendOpId = Shader.PropertyToID("_BlendOp");
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

            {
                const int resolution = 32;
                const TextureFormat textureFormat = TextureFormat.R8;
                var texture = new Texture2D(resolution, resolution, textureFormat, false, true);

                int depthRt = Shader.PropertyToID("DepthRT");
                cmd.GetTemporaryRT(depthRt, resolution, resolution, 32, FilterMode.Point, RenderTextureFormat.Depth);
                int tempRt = Shader.PropertyToID("TempRT");
                cmd.GetTemporaryRT(tempRt, resolution, resolution, 0, FilterMode.Point, RenderTextureFormat.R8);

                var viewMatrix = Matrix4x4.Inverse(
                    Matrix4x4.TRS(Vector3.zero, Quaternion.LookRotation(Vector3.down, Vector3.forward), Vector3.one)
                );
                var projectionMatrix = Matrix4x4.Ortho(-2.0f, 2.0f, -2.0f, 2.0f, -100.0f, 100.0f);

                cmd.SetRenderTarget(depthRt);
                cmd.ClearRenderTarget(true, false, Color.black);
                cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

                foreach (MeshRenderer meshRenderer in _settings.Blobs.Model.GetComponentsInChildren<MeshRenderer>())
                {
                    MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                    Material[] sharedMaterials = meshRenderer.sharedMaterials;
                    int subMeshCount = Mathf.Min(sharedMaterials.Length, meshFilter.sharedMesh.subMeshCount);
                    for (int index = 0; index < subMeshCount; index++)
                    {
                        Material material = sharedMaterials[index];
                        cmd.DrawRenderer(meshRenderer, material, index, 2);
                    }
                }

                cmd.SetRenderTarget(tempRt);
                ToonBlitter.BlitDefault(cmd, depthRt);

                cmd.CopyTexture(tempRt, texture);

                // RenderTexture.ReleaseTemporary(depthRt);
                // RenderTexture.ReleaseTemporary(tempRt);
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
                    cmd.DrawMesh(mesh, Matrix4x4.identity, _material, SubmeshIndex, shadowType);
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