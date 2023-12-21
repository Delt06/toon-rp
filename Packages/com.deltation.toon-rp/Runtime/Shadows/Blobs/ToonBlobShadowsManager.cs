using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using static DELTation.ToonRP.Shadows.Blobs.ToonBlobShadowsBatching;

namespace DELTation.ToonRP.Shadows.Blobs
{
    [ExecuteAlways]
    public sealed unsafe class ToonBlobShadowsManager : MonoBehaviour
    {
        public Group[] AllGroups { get; private set; } = Array.Empty<Group>();

        public bool IsDestroyed { get; private set; }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            Destroy();
        }

        public void Init() { }

        public void EnsureInitialized()
        {
            if (!IsDestroyed && AllGroups.Length == 0)
            {
                AllGroups = new Group[ToonBlobShadowTypes.Count];

                for (int shadowType = 0; shadowType < ToonBlobShadowTypes.Count; shadowType++)
                {
                    AllGroups[shadowType] = new Group((ToonBlobShadowType) shadowType);
                }
            }
        }

        internal void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }

            IsDestroyed = true;

            foreach (Group group in AllGroups)
            {
                if (group == null)
                {
                    continue;
                }

                foreach (ToonBlobShadowRenderer r in group.Renderers)
                {
                    if (r != null)
                    {
                        r.UnassignFromManager();
                    }
                }

                group.Dispose();
            }

            AllGroups = Array.Empty<Group>();

            ToonBlobShadowsManagers.OnDestroyed(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Group GetGroup(ToonBlobShadowType type) => AllGroups[(int) type];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetGroup(ToonBlobShadowType type, out Group group)
        {
            int typeIndex = (int) type;
            if (typeIndex < AllGroups.Length)
            {
                group = AllGroups[typeIndex];
                return true;
            }

            group = default;
            return false;
        }

        [SuppressMessage("ReSharper", "NotAccessedField.Global")]
        public struct RendererPackedData
        {
            public half4 PositionSize;
            public ToonBlobShadowsPackedParams Params;
        }

        public class Group : IDisposable
        {
            private const int StartSize = MaxBatchSize;

            public readonly List<ToonBlobShadowRenderer> DynamicRenderers = new();
            public readonly List<ToonBlobShadowRenderer> Renderers = new();
            public readonly ToonBlobShadowType ShadowType;

            private bool _isDataDirty = true;
            private NativeArray<RendererPackedData> _packedData = new(StartSize,
                Allocator.Persistent, NativeArrayOptions.UninitializedMemory
            );

            public NativeArray<ToonBlobShadowsRendererData> Data = new(StartSize, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory
            );

            public Group(ToonBlobShadowType shadowType) => ShadowType = shadowType;

            public ToonBlobShadowsRendererData* DataPtr => (ToonBlobShadowsRendererData*) Data.GetUnsafePtr();
            public RendererPackedData* PackedDataPtr => (RendererPackedData*) _packedData.GetUnsafePtr();

            public GraphicsBuffer PackedDataConstantBuffer { get; private set; } = CreateConstantBuffer(StartSize);

            public void Dispose()
            {
                Renderers.Clear();
                DynamicRenderers.Clear();
                _packedData.Dispose();
                _packedData = default;
                PackedDataConstantBuffer.Release();
                Data.Dispose();
            }

            private static GraphicsBuffer CreateConstantBuffer(int size)
            {
                // Align to the max batch size
                size = (size + MaxBatchSize - 1) / MaxBatchSize * MaxBatchSize;
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
                    _isDataDirty = false;
                }
            }
        }
    }
}