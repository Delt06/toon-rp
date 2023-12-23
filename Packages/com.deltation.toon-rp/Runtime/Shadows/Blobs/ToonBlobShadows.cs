using System;
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
        private static readonly ProfilerMarker AwaitCullingMarker =
            new("BlobShadows.AwaitCulling");

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
            cmd.GetTemporaryRT(ShaderIds.ShadowMap, atlasSize, atlasSize, 0, FilterMode.Bilinear,
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
                cmd.SetRenderTarget(ShaderIds.ShadowMap,
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

                    {
                        // Apply the position offset to the bounds.
                        // The offset is applied during shadow rendering, thus it is inverted here.
                        float2 shadowPositionOffset = -_settings.Blobs.ShadowPositionOffset;
                        receiverBounds.Min = math.min(receiverBounds.Min + shadowPositionOffset, receiverBounds.Min);
                        receiverBounds.Max = math.max(receiverBounds.Max + shadowPositionOffset, receiverBounds.Max);
                    }

                    // Slight padding to ensure shadows do not touch the shadowmap bounds.
                    // Otherwise, there may be artifacts on low resolutions (< 128). 
                    receiverBounds.Size *= 1.01f;

                    CollectManagers();

                    {
                        float2 min = receiverBounds.Min;
                        float2 size = receiverBounds.Size;
                        var minSize = new Vector4(
                            -min.x, -min.y,
                            1.0f / size.x, 1.0f / size.y
                        );
                        cmd.SetGlobalVector(ShaderIds.MinSize, minSize);

                        cmd.SetGlobalVector(ShaderIds.Offset, _settings.Blobs.ShadowPositionOffset);
                    }

                    DoRender(cmd, receiverBounds);

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

        private void DoRender(CommandBuffer cmd, in Bounds2D receiverBounds)
        {
            foreach (ToonBlobShadowsManager manager in _managers)
            {
                foreach (ToonBlobShadowsManager.Group group in manager.AllGroups)
                {
                    // Don't even check the bounds: they are irrelevant for default groups
                    _batching.Batch(group);
                }

                foreach (ToonBlobShadowsGroup customGroup in manager.CustomGroups)
                {
                    if (receiverBounds.Intersects(customGroup.Bounds))
                    {
                        _batching.Batch(customGroup);
                    }
                }
            }

            ToonBlobShadowsCullingHandle cullingHandle = Cull(receiverBounds);

            if (!cullingHandle.IsEmpty)
            {
                DrawShadows(cmd, cullingHandle);
            }

            cullingHandle.Dispose();
            _batching.Clear();
        }

        private ToonBlobShadowsCullingHandle Cull(in Bounds2D receiverBounds)
        {
            using (AwaitCullingMarker.Auto())
            {
                ToonBlobShadowsCullingHandle cullingHandle =
                    ToonBlobShadowsCulling.ScheduleCulling(_batching, receiverBounds);
                cullingHandle.Complete();
                return cullingHandle;
            }
        }

        private unsafe void DrawShadows(CommandBuffer cmd, ToonBlobShadowsCullingHandle cullingHandle)
        {
            _material.SetFloat(ShaderIds.Saturation, _blobShadowsSettings.Saturation);
            SetupBlending();

            int* sharedVisibleIndicesPtr = (int*) cullingHandle.SharedIndices.GetUnsafePtr();
            int* sharedVisibleCountersPtr = (int*) cullingHandle.SharedCounters.GetUnsafePtr();

            using (DrawBatchesMarker.Auto())
            {
                ToonBlobShadowsAtlas atlas = _settings.Blobs.BakedShadowsAtlas;
                if (atlas != null && atlas.Texture != null && atlas.TilingOffsets != null)
                {
                    cmd.SetGlobalTexture(ShaderIds.BakedTexturesAtlas, atlas.Texture);
                    Array.Copy(atlas.TilingOffsets, SharedBuffers.TilingOffsets, atlas.TilingOffsets.Length);
                }
                else
                {
                    cmd.SetGlobalTexture(ShaderIds.BakedTexturesAtlas, Texture2D.whiteTexture);
                    SharedBuffers.TilingOffsets[0] = new Vector4(1, 1, 0, 0);
                }

                cmd.SetGlobalVectorArray(ShaderIds.BakedTexturesAtlasTilingOffsets, SharedBuffers.TilingOffsets);

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
                                int visibleRenderersCount = sharedVisibleCountersPtr[batch.CullingGroupIndex];
                                if (visibleRenderersCount == 0)
                                {
                                    continue;
                                }

                                int packedDataStride =
                                    UnsafeUtility.SizeOf<ToonBlobShadowPackedData>();
                                GraphicsBuffer constantBuffer = batch.Group.PackedDataConstantBuffer;

                                int startAddress = batch.BaseIndex * packedDataStride;
                                // On WebGL, non-full batches may cause errors if we don't bind the full batch's worth of data.
                                // "GL_INVALID_OPERATION: It is undefined behaviour to use a uniform buffer that is too small."
                                int endAddress = startAddress + ToonBlobShadowsBatching.MaxBatchSize * packedDataStride;
                                endAddress = Mathf.Min(endAddress, constantBuffer.count * constantBuffer.stride);

                                cmd.SetGlobalConstantBuffer(constantBuffer,
                                    ShaderIds.PackedData,
                                    startAddress,
                                    endAddress - startAddress
                                );

                                int* indicesBeginPtr = sharedVisibleIndicesPtr +
                                                       batch.CullingGroupIndex * ToonBlobShadowsBatching.MaxBatchSize;

                                ToonUnsafeUtility.MemcpyToManagedArray(
                                    SharedBuffers.IndicesBuffer, indicesBeginPtr, visibleRenderersCount
                                );
                                cmd.SetGlobalFloatArray(ShaderIds.Indices, SharedBuffers.IndicesBuffer);

                                int shaderPass = (int) batchSet.ShadowType;
                                cmd.DrawProcedural(Matrix4x4.identity, _material, shaderPass, MeshTopology.Quads,
                                    4 * visibleRenderersCount
                                );
                            }
                        }
                    }
                }
            }
        }

        private void SetupBlending()
        {
            (UnityBlendMode srcBlend, UnityBlendMode dstBlend, BlendOp blendOp) = _blobShadowsSettings.Mode switch
            {
                ToonBlobShadowsMode.MetaBalls => (UnityBlendMode.SrcColor, UnityBlendMode.One, BlendOp.Add),
                ToonBlobShadowsMode.Default => (UnityBlendMode.One, UnityBlendMode.One, BlendOp.Max),
                _ => throw new ArgumentOutOfRangeException(),
            };
            _material.SetFloat(ShaderIds.SrcBlend, (float) srcBlend);
            _material.SetFloat(ShaderIds.DstBlend, (float) dstBlend);
            _material.SetFloat(ShaderIds.BlendOp, (float) blendOp);
        }

        public void Cleanup()
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.ReleaseTemporaryRT(ShaderIds.ShadowMap);
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
            public static readonly int ShadowMap = Shader.PropertyToID("_ToonRP_BlobShadowMap");
            public static readonly int MinSize = Shader.PropertyToID("_ToonRP_BlobShadows_Min_Size");
            public static readonly int Offset = Shader.PropertyToID("_ToonRP_BlobShadows_Offset");
            public static readonly int Saturation = Shader.PropertyToID("_Saturation");
            public static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
            public static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
            public static readonly int BlendOp = Shader.PropertyToID("_BlendOp");

            public static readonly int BakedTexturesAtlas =
                Shader.PropertyToID("_ToonRP_BlobShadows_BakedTexturesAtlas");
            public static readonly int BakedTexturesAtlasTilingOffsets =
                Shader.PropertyToID("_ToonRP_BlobShadows_BakedTexturesAtlas_TilingOffsets");
            public static readonly int PackedData = Shader.PropertyToID("_ToonRP_BlobShadows_PackedData");
            public static readonly int Indices = Shader.PropertyToID("_Indices");
        }
    }
}