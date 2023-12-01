using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Shadows.Blobs
{
    public unsafe class ToonBlobShadowBrgContainer : IDisposable
    {
        private static readonly int WorldBoundsId = Shader.PropertyToID("_WorldBounds");
        private static readonly int ShadowTypeParamsId = Shader.PropertyToID("_ShadowTypeParams");

        private int _alignedGPUWindowSize;
        private BatchID[] _batchIDs;
        private BatchRendererGroup _brg;
        private NativeArray<float4> _cpuPersistentInstanceData;
        private int _globalDataSize;
        private GraphicsBuffer _gpuPersistentInstanceData;
        private bool _initialized;
        private int _instanceCount;
        private int _instanceSize;
        private BatchMaterialID _materialId;
        private int _maxInstancePerWindow;
        private int _maxInstances;
        private BatchMeshID _meshId;
        private int _totalGpuBufferSize;
        private int _windowCount;

        // In GLES mode, BRG raw buffer is a constant buffer (UBO)
        private static bool UseConstantBuffer => BatchRendererGroup.BufferTarget == BatchBufferTarget.ConstantBuffer;

        public void Dispose()
        {
            if (_initialized)
            {
                for (uint batchIndex = 0; batchIndex < _windowCount; batchIndex++)
                {
                    _brg.RemoveBatch(_batchIDs[batchIndex]);
                }

                _brg.UnregisterMaterial(_materialId);
                _brg.UnregisterMesh(_meshId);
                _brg.Dispose();
                _gpuPersistentInstanceData.Dispose();
                _cpuPersistentInstanceData.Dispose();
            }
        }

        public void Init(Mesh mesh, Material material, int maxInstances)
        {
            _brg = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
            _globalDataSize = Marshal.SizeOf<float4>();
            _instanceSize = Marshal.SizeOf<float4>();
            _maxInstances = maxInstances;
            _instanceCount = 0;

            // Create GPU persistent buffer
            if (UseConstantBuffer)
            {
                _alignedGPUWindowSize = BatchRendererGroup.GetConstantBufferMaxWindowSize();
                _maxInstancePerWindow = (_alignedGPUWindowSize - _globalDataSize) / _instanceSize;
                _windowCount = (_maxInstances + _maxInstancePerWindow - 1) / _maxInstancePerWindow;
                _totalGpuBufferSize = _windowCount * _alignedGPUWindowSize;
                _gpuPersistentInstanceData =
                    new GraphicsBuffer(GraphicsBuffer.Target.Constant, _totalGpuBufferSize / 16, 16);
            }
            else
            {
                _alignedGPUWindowSize = (_maxInstances * _instanceSize + _globalDataSize + 15) & -16;
                _maxInstancePerWindow = maxInstances;
                _windowCount = 1;
                _totalGpuBufferSize = _windowCount * _alignedGPUWindowSize;
                _gpuPersistentInstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Raw, _totalGpuBufferSize / 4, 4);
            }

            // Create batches
            {
                // Metadata:
                // global: float4 world bounds
                // per-instance: float4 params
                var batchMetadata =
                    new NativeArray<MetadataValue>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

                _batchIDs = new BatchID[_windowCount];
                _cpuPersistentInstanceData = new NativeArray<float4>(
                    _totalGpuBufferSize / Marshal.SizeOf<float4>(), Allocator.Persistent
                );

                for (int batchIndex = 0; batchIndex < _windowCount; batchIndex++)
                {
                    batchMetadata[0] = CreateMetadataValue(WorldBoundsId, 0, false);
                    batchMetadata[1] = CreateMetadataValue(ShadowTypeParamsId, 0 + _globalDataSize, true);

                    int offset = batchIndex * _alignedGPUWindowSize;
                    _batchIDs[batchIndex] = _brg.AddBatch(batchMetadata, _gpuPersistentInstanceData.bufferHandle,
                        (uint) offset,
                        UseConstantBuffer ? (uint) _alignedGPUWindowSize : 0
                    );
                }

                batchMetadata.Dispose();
            }

            // large enough to avoid culling the BRG
            var bounds = new Bounds(Vector3.zero, Vector3.one * 1048576.0f);
            _brg.SetGlobalBounds(bounds);

            _meshId = _brg.RegisterMesh(mesh);
            _materialId = _brg.RegisterMaterial(material);

            _initialized = true;
        }

        [BurstCompile]
        public bool TryUploadGpuData(int instanceCount)
        {
            if (instanceCount > _maxInstances)
            {
                return false;
            }

            _instanceCount = instanceCount;
            int wholeWindows = _instanceCount / _maxInstancePerWindow;

            if (wholeWindows > 0)
            {
                int windowElements = wholeWindows * _alignedGPUWindowSize / Marshal.SizeOf<float4>();
                _gpuPersistentInstanceData.SetData(_cpuPersistentInstanceData, 0, 0, windowElements);
            }

            int lastBatchId = wholeWindows;
            int itemsInLastBatch = _instanceCount % _maxInstancePerWindow;

            if (itemsInLastBatch > 0)
            {
                int windowOffsetInElements = lastBatchId * _alignedGPUWindowSize / Marshal.SizeOf<float4>();
                int offsetInElements = windowOffsetInElements;

                // 1 global float4
                _gpuPersistentInstanceData.SetData(_cpuPersistentInstanceData, offsetInElements, offsetInElements, 1);
                offsetInElements += _globalDataSize / Marshal.SizeOf<float4>();

                // 1 per-instance float4
                _gpuPersistentInstanceData.SetData(_cpuPersistentInstanceData, offsetInElements, offsetInElements,
                    itemsInLastBatch
                );
            }

            return true;
        }

        public NativeArray<float4> GetCpuPersistentInstanceData(out int totalSize, out int alignedWindowSize)
        {
            totalSize = _totalGpuBufferSize;
            alignedWindowSize = _alignedGPUWindowSize;
            return _cpuPersistentInstanceData;
        }

        private static MetadataValue CreateMetadataValue(int nameID, int gpuOffset, bool isPerInstance)
        {
            const uint kIsPerInstanceBit = 0x80000000;
            return new MetadataValue
            {
                NameID = nameID,
                Value = (uint) gpuOffset | (isPerInstance ? kIsPerInstanceBit : 0),
            };
        }

        // Helper function to allocate BRG buffers during the BRG callback function
        private static T* Malloc<T>(uint count) where T : unmanaged =>
            (T*) UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<T>() * count,
                UnsafeUtility.AlignOf<T>(),
                Allocator.TempJob
            );

        private JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext,
            BatchCullingOutput cullingOutput, IntPtr userContext)
        {
            if (_initialized)
            {
                var drawCommands = new BatchCullingOutputDrawCommands();

                // amount of draw commands in case of UBO mode
                int drawCommandsCount = (_instanceCount + _maxInstancePerWindow - 1) / _maxInstancePerWindow;
                int maxInstancesPerDrawCommand = _maxInstancePerWindow;
                drawCommands.drawCommandCount = drawCommandsCount;

                drawCommands.drawRangeCount = 1;
                drawCommands.drawRanges = Malloc<BatchDrawRange>(1);
                drawCommands.drawRanges[0] = new BatchDrawRange
                {
                    drawCommandsBegin = 0,
                    drawCommandsCount = (uint) drawCommandsCount,
                    filterSettings = new BatchFilterSettings
                    {
                        renderingLayerMask = 1,
                        layer = 0,
                        motionMode = MotionVectorGenerationMode.ForceNoMotion,
                        shadowCastingMode = ShadowCastingMode.Off,
                        receiveShadows = false,
                        staticShadowCaster = false,
                        allDepthSorted = false,
                    },
                };

                if (drawCommands.drawCommandCount > 0)
                {
                    // as we don't need culling, the visibility int array buffer will always be {0,1,2,3,...} for each draw command
                    // so we just allocate maxInstancePerDrawCommand and fill it
                    int visibilityArraySize = maxInstancesPerDrawCommand;
                    if (_instanceCount < visibilityArraySize)
                    {
                        visibilityArraySize = _instanceCount;
                    }

                    drawCommands.visibleInstances = Malloc<int>((uint) visibilityArraySize);

                    for (int i = 0; i < visibilityArraySize; i++)
                    {
                        drawCommands.visibleInstances[i] = i;
                    }

                    // Allocate the BatchDrawCommand array (drawCommandsCount entries)
                    // In SSBO mode, drawCommandCount will be just 1
                    drawCommands.drawCommands = Malloc<BatchDrawCommand>((uint) drawCommandsCount);
                    int left = _instanceCount;

                    for (int batchIndex = 0; batchIndex < drawCommandsCount; batchIndex++)
                    {
                        int inBatchCount = math.min(left, maxInstancesPerDrawCommand);
                        drawCommands.drawCommands[batchIndex] = new BatchDrawCommand
                        {
                            visibleOffset =
                                0, // all draw command is using the same {0,1,2,3...} visibility int array
                            visibleCount = (uint) inBatchCount,
                            batchID = _batchIDs[batchIndex],
                            materialID = _materialId,
                            meshID = _meshId,
                            submeshIndex = 0,
                            splitVisibilityMask = 0xFF,
                            flags = BatchDrawCommandFlags.None,
                            sortingPosition = 0,
                        };

                        left -= inBatchCount;
                    }
                }

                cullingOutput.drawCommands[0] = drawCommands;
                drawCommands.instanceSortingPositions = null;
                drawCommands.instanceSortingPositionFloatCount = 0;
            }

            return new JobHandle();
        }
    }
}