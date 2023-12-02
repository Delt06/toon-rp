using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityBlendMode = UnityEngine.Rendering.BlendMode;

namespace DELTation.ToonRP.Shadows.Blobs
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

        private static readonly ProfilerMarker DrawBatchesMarker =
            new("BlobShadows.DrawBatches");
        private readonly ToonBlobShadowsBatching _batching = new();
        private readonly ToonBlobShadowsCulling _culling = new();
        private readonly List<ToonBlobShadowsManager> _managers = new();

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


                CollectManagers();
                float maxDistance = Mathf.Min(_settings.MaxDistance, _camera.farClipPlane);
                _culling.Cull(_managers, _settings.Blobs, _camera, maxDistance);

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

                DrawShadows(cmd);

                _managers.Clear();
                _culling.Clear();
            }

            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void CollectManagers()
        {
            _managers.Clear();

            if (_camera.cameraType == CameraType.Game)
            {
                var manager = ToonBlobShadowsManager.GetManager(_camera);

                if (manager != null)
                {
                    _managers.Add(manager);
                }
            }
            else if (_camera.cameraType == CameraType.SceneView)
            {
                foreach (ToonBlobShadowsManager manager in ToonBlobShadowsManager.AllManagers)
                {
                    _managers.Add(manager);
                }
            }
        }

        private void DrawShadows(CommandBuffer cmd)
        {
            _material.SetFloat(SaturationId, _blobShadowsSettings.Saturation);
            SetupBlending();

            foreach ((ToonBlobShadowsManager manager, List<int> indices) in _culling.VisibleRenderers)
            {
                _batching.Batch(manager, indices);
            }

            using (DrawBatchesMarker.Auto())
            {
                for (int i = 0; i < _batching.BatchCount; i++)
                {
                    ref readonly ToonBlobShadowsBatching.BatchData batch = ref _batching.Batches[i];

                    Texture2D bakedShadowTexture = batch.Key.BakedTexture;

                    cmd.SetGlobalVectorArray("_ToonRP_BlobShadows_Positions", batch.Positions);
                    cmd.SetGlobalVectorArray("_ToonRP_BlobShadows_Params", batch.Params);

                    if (bakedShadowTexture)
                    {
                        cmd.SetGlobalTexture(BakedBlobShadowTextureId, bakedShadowTexture);
                    }

                    int shaderPass = (int) batch.Key.ShadowType;
                    cmd.DrawProcedural(Matrix4x4.identity, _material, shaderPass, MeshTopology.Quads,
                        4 * batch.Positions.Count
                    );
                }
            }

            _batching.Clear();
        }

        private void SetupBlending()
        {
            (UnityBlendMode srcBlend, UnityBlendMode dstBlend, BlendOp blendOp) = _blobShadowsSettings.Mode switch
            {
                ToonBlobShadowsMode.MetaBalls => (UnityBlendMode.SrcColor, UnityBlendMode.One, BlendOp.Add),
                ToonBlobShadowsMode.Default => (UnityBlendMode.One, UnityBlendMode.One, BlendOp.Max),
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