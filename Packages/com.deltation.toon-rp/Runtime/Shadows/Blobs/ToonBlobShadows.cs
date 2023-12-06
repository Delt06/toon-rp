﻿using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityBlendMode = UnityEngine.Rendering.BlendMode;

namespace DELTation.ToonRP.Shadows.Blobs
{
    public class ToonBlobShadows : IDisposable
    {
        public const int MaxBakedTextures = 64;

        public const string ShaderName = "Hidden/Toon RP/Blob Shadow Pass";

        private static readonly ProfilerMarker DrawBatchesMarker =
            new("BlobShadows.DrawBatches");
        private static readonly int IndicesId = Shader.PropertyToID("_ToonRP_BlobShadows_Indices");

        private readonly ToonBlobShadowsBatching _batching = new();
        private readonly List<ToonBlobShadowsManager> _managers = new();

        private ToonBlobShadowsSettings _blobShadowsSettings;
        private Camera _camera;
        private ScriptableRenderContext _context;
        private Material _material;
        private ToonShadowSettings _settings;

        public void Dispose() { }

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
            cmd.GetTemporaryRT(ShaderIds.ShadowMapId, atlasSize, atlasSize, 0, FilterMode.Bilinear,
                RenderTextureFormat.R8,
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
                cmd.SetRenderTarget(ShaderIds.ShadowMapId,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                );
                cmd.ClearRenderTarget(false, true, Color.black);

                Bounds2D? intersection = FrustumPlaneProjectionUtils.ComputeFrustumPlaneIntersection(_camera,
                    _settings.MaxDistance,
                    _settings.Blobs.ReceiverPlaneY
                );

                if (intersection.HasValue)
                {
                    Bounds2D receiverBounds = intersection.Value;

                    // slight padding to ensure shadows do not touch the shadowmap bounds
                    // otherwise, there may be artifacts on low resolutions (< 128) 
                    receiverBounds.Size *= 1.01f;

                    CollectManagers();

                    {
                        float2 min = receiverBounds.Min;
                        float2 size = receiverBounds.Size;
                        var minSize = new Vector4(
                            min.x, min.y,
                            size.x, size.y
                        );
                        cmd.SetGlobalVector(ShaderIds.MinSizeId, minSize);
                    }

                    cmd.SetGlobalVector(ShaderIds.CoordsOffsetId, _settings.Blobs.ShadowPositionOffset);

                    DrawShadows(cmd, receiverBounds);

                    _managers.Clear();
                }
            }

            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void CollectManagers()
        {
            _managers.Clear();

            if (_camera.cameraType == CameraType.Game)
            {
                ToonBlobShadowsManager manager = ToonBlobShadowsManagers.Get(_camera);

                if (manager != null)
                {
                    _managers.Add(manager);
                }
            }
            else if (_camera.cameraType == CameraType.SceneView)
            {
                foreach (ToonBlobShadowsManager manager in ToonBlobShadowsManagers.All)
                {
                    _managers.Add(manager);
                }
            }
        }

        private void DrawShadows(CommandBuffer cmd, in Bounds2D receiverBounds)
        {
            _material.SetFloat(ShaderIds.SaturationId, _blobShadowsSettings.Saturation);
            SetupBlending();

            foreach (ToonBlobShadowsManager manager in _managers)
            {
                foreach (ToonBlobShadowsManager.Group group in manager.AllGroups)
                {
                    _batching.Batch(group, receiverBounds);
                }
            }

            using (DrawBatchesMarker.Auto())
            {
                ToonBlobShadowsAtlas atlas = _settings.Blobs.BakedShadowsAtlas;
                if (atlas != null && atlas.Texture != null && atlas.TilingOffsets != null)
                {
                    cmd.SetGlobalTexture(ShaderIds.BakedTexturesAtlasId, atlas.Texture);
                    Array.Copy(atlas.TilingOffsets, SharedBuffers.TilingOffsets, atlas.TilingOffsets.Length);
                }
                else
                {
                    cmd.SetGlobalTexture(ShaderIds.BakedTexturesAtlasId, Texture2D.whiteTexture);
                    SharedBuffers.TilingOffsets[0] = new Vector4(1, 1, 0, 0);
                }

                cmd.SetGlobalVectorArray(ShaderIds.BakedTexturesAtlasTilingOffsetsId, SharedBuffers.TilingOffsets);

                for (int shadowTypeIndex = 0; shadowTypeIndex < ToonBlobShadowTypes.Count; shadowTypeIndex++)
                {
                    ToonBlobShadowsBatching.BatchSet batchSet =
                        _batching.GetBatches((ToonBlobShadowType) shadowTypeIndex);

                    if (batchSet.BatchCount > 0)
                    {
                        using (new ProfilingScope(cmd,
                                   NamedProfilingSampler.Get(ToonBlobShadowTypes.Names[shadowTypeIndex])
                               ))
                        {
                            for (int batchIndex = 0; batchIndex < batchSet.BatchCount; batchIndex++)
                            {
                                ref ToonBlobShadowsBatching.BatchData batch = ref batchSet.Batches[batchIndex];
                                batch.CullingJobHandle.Complete();
                                int visibleRenderersCount = batch.VisibleIndices.Length;
                                if (visibleRenderersCount == 0)
                                {
                                    continue;
                                }

                                int packedDataStride =
                                    UnsafeUtility.SizeOf<ToonBlobShadowsManager.RendererPackedData>();
                                cmd.SetGlobalConstantBuffer(batch.Group.PackedDataConstantBuffer,
                                    ShaderIds.PackedDataId,
                                    batch.BaseIndex * packedDataStride,
                                    batch.Count * packedDataStride
                                );

                                ToonUnsafeUtility.MemcpyToManagedArray(
                                    SharedBuffers.IndicesBuffer, batch.VisibleIndices
                                );
                                cmd.SetGlobalFloatArray(IndicesId, SharedBuffers.IndicesBuffer);

                                int shaderPass = (int) batchSet.ShadowType;
                                cmd.DrawProcedural(Matrix4x4.identity, _material, shaderPass, MeshTopology.Quads,
                                    4 * visibleRenderersCount
                                );
                            }
                        }
                    }
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
            _material.SetFloat(ShaderIds.SrcBlendId, (float) srcBlend);
            _material.SetFloat(ShaderIds.DstBlendId, (float) dstBlend);
            _material.SetFloat(ShaderIds.BlendOpId, (float) blendOp);
        }

        public void Cleanup()
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.ReleaseTemporaryRT(ShaderIds.ShadowMapId);
            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        private static class SharedBuffers
        {
            public static readonly Vector4[] TilingOffsets = new Vector4[MaxBakedTextures];
            public static readonly float[] IndicesBuffer = new float[ToonBlobShadowsBatching.MaxBatchSize];
        }

        private static class ShaderIds
        {
            public static readonly int ShadowMapId = Shader.PropertyToID("_ToonRP_BlobShadowMap");
            public static readonly int MinSizeId = Shader.PropertyToID("_ToonRP_BlobShadows_Min_Size");
            public static readonly int CoordsOffsetId = Shader.PropertyToID("_ToonRP_BlobShadowCoordsOffset");
            public static readonly int SaturationId = Shader.PropertyToID("_Saturation");
            public static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
            public static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
            public static readonly int BlendOpId = Shader.PropertyToID("_BlendOp");

            public static readonly int BakedTexturesAtlasId =
                Shader.PropertyToID("_ToonRP_BlobShadows_BakedTexturesAtlas");
            public static readonly int BakedTexturesAtlasTilingOffsetsId =
                Shader.PropertyToID("_ToonRP_BlobShadows_BakedTexturesAtlas_TilingOffsets");
            public static readonly int PackedDataId = Shader.PropertyToID("_ToonRP_BlobShadows_PackedData");
        }
    }
}