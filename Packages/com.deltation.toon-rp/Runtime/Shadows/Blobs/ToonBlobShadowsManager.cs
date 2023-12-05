using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace DELTation.ToonRP.Shadows.Blobs
{
    [ExecuteAlways]
    public sealed unsafe class ToonBlobShadowsManager : MonoBehaviour
    {
        private Group[] _groups;

        private void Awake()
        {
            _groups = new Group[ToonBlobShadowTypes.Count];

            for (int i = 0; i < _groups.Length; i++)
            {
                _groups[i] = new Group();
            }
        }

        private void OnDestroy()
        {
            foreach (Group group in _groups)
            {
                group?.Dispose();
            }

            _groups = null;

            ToonBlobShadowsManagers.OnDestroyed(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Group GetGroup(ToonBlobShadowType type) => _groups[(int) type];

        public struct RendererPackedData
        {
            public float4 PositionSize;
            public float4 Params;
        }

        public class Group : IDisposable
        {
            private const int StartSize = 128;

            public readonly List<ToonBlobShadowRenderer> DynamicRenderers = new();
            public readonly List<ToonBlobShadowRenderer> Renderers = new();

            private bool _isDataDirty = true;
            private NativeArray<RendererPackedData> _packedData = new(StartSize,
                Allocator.Persistent, NativeArrayOptions.UninitializedMemory
            );

            public NativeArray<ToonBlobShadowsRendererData> Data = new(StartSize, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory
            );

            public ToonBlobShadowsRendererData* DataPtr => (ToonBlobShadowsRendererData*) Data.GetUnsafePtr();
            public RendererPackedData* PackedDataPtr => (RendererPackedData*) _packedData.GetUnsafePtr();

            public GraphicsBuffer PackedDataConstantBuffer { get; private set; } = CreateConstantBuffer(StartSize);

            public void Dispose()
            {
                Renderers.Clear();
                DynamicRenderers.Clear();
                _packedData.Dispose();
                PackedDataConstantBuffer.Release();
                Data.Dispose();
            }

            private static GraphicsBuffer CreateConstantBuffer(int size)
            {
                int stride = UnsafeUtility.SizeOf<float4>();
                return new GraphicsBuffer(GraphicsBuffer.Target.Constant,
                    size * UnsafeUtility.SizeOf<RendererPackedData>() / stride
                    , stride
                );
            }

            public void MarkDataDirty()
            {
                _isDataDirty = true;
            }

            public void ExpandData()
            {
                ExpandArray(ref Data);
                ExpandArray(ref _packedData);

                GraphicsBuffer newGpuData = CreateConstantBuffer(Data.Length * 2);
                PackedDataConstantBuffer.Release();
                PackedDataConstantBuffer = newGpuData;

                MarkDataDirty();
            }

            private static void ExpandArray<T>(ref NativeArray<T> array) where T : struct
            {
                var newArray = new NativeArray<T>(array.Length * 2, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory
                );
                UnsafeUtility.MemCpy(newArray.GetUnsafePtr(), array.GetUnsafePtr(),
                    UnsafeUtility.SizeOf<T>() * array.Length
                );
                array.Dispose();
                array = newArray;
            }

            public void UpdateRendererData()
            {
                foreach (ToonBlobShadowRenderer dynamicRenderer in DynamicRenderers)
                {
                    if (dynamicRenderer == null)
                    {
                        continue;
                    }

                    dynamicRenderer.UpdateRendererData(out bool changed);
                    if (changed)
                    {
                        _isDataDirty = true;
                    }
                }

                if (_isDataDirty)
                {
                    PackedDataConstantBuffer.SetData(_packedData, 0, 0, Renderers.Count);
                    _isDataDirty = true;
                }
            }
        }
    }
}