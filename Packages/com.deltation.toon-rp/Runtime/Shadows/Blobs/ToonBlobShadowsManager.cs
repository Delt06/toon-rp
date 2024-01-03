using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;
using static DELTation.ToonRP.Shadows.Blobs.ToonBlobShadowsBatching;

namespace DELTation.ToonRP.Shadows.Blobs
{
    [ExecuteAlways]
    public sealed unsafe class ToonBlobShadowsManager : MonoBehaviour
    {
        internal Group[] AllGroups { get; private set; } = Array.Empty<Group>();

        internal List<ToonBlobShadowsGroup> CustomGroups { get; } = new();

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

                foreach (ToonBlobShadowRenderer r in group.AllRenderers())
                {
                    if (r != null)
                    {
                        r.UnassignFromManager();
                    }
                }

                group.Dispose();
            }

            AllGroups = Array.Empty<Group>();

            foreach (ToonBlobShadowsGroup extraGroup in CustomGroups)
            {
                extraGroup.Dispose();
            }

            CustomGroups.Clear();
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

        public class Group : IDisposable
        {
            private const int StartSize = MaxBatchSize;
            private const Allocator DataAllocator = Allocator.Persistent;
            private const NativeArrayOptions DefaultArrayOptions = NativeArrayOptions.UninitializedMemory;

            private readonly List<ToonBlobShadowRenderer> _dynamicRenderers = new();
            private readonly List<ToonBlobShadowRenderer> _renderers = new();

            internal readonly ToonBlobShadowsGroup InnerGroup;


            private NativeArray<ToonBlobShadowsRendererData> _data = new(StartSize, DataAllocator, DefaultArrayOptions);
            private bool _isDataDirty = true;

            public Group(ToonBlobShadowType shadowType) => InnerGroup =
                new ToonBlobShadowsGroup(shadowType, Bounds2D.FromCenterExtents(0.0f, 100_000_000.0f));

            public int Size
            {
                get => InnerGroup.Size;
                private set => InnerGroup.Size = value;
            }

            public ToonBlobShadowsRendererData* DataPtr => (ToonBlobShadowsRendererData*) _data.GetUnsafePtr();
            public ToonBlobShadowPackedData* PackedDataPtr => InnerGroup.PackedDataPtr;

            public void Dispose()
            {
                _renderers.Clear();
                _dynamicRenderers.Clear();
                InnerGroup.Dispose();
                _data.Dispose();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public List<ToonBlobShadowRenderer> AllRenderers() => _renderers;

            public void UpdateRendererData()
            {
                foreach (ToonBlobShadowRenderer dynamicRenderer in _dynamicRenderers)
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
                    InnerGroup.PushDataToGPU();
                    _isDataDirty = false;
                }
            }

            public void AddRenderer(ToonBlobShadowRenderer renderer)
            {
                _renderers.Add(renderer);

                if (!renderer.IsStatic || ToonBlobShadowRenderer.ForceUpdateRenderers)
                {
                    _dynamicRenderers.Add(renderer);
                }

                int newSize = Size + 1;

                if (_data.Length < newSize)
                {
                    ExpandData();
                }

                Size = newSize;

                MarkDataDirty();
            }

            public void RemoveRenderer(ToonBlobShadowRenderer renderer)
            {
                Assert.IsTrue(0 <= renderer.Index && renderer.Index < Size);

                int lastIndex = Size - 1;
                if (renderer.Index == lastIndex)
                {
                    _renderers.RemoveAt(renderer.Index);
                }
                else
                {
                    // Swap with the last renderer and remove
                    ToonBlobShadowRenderer lastRenderer = _renderers[lastIndex];
                    _renderers[renderer.Index] = lastRenderer;
                    _renderers.RemoveAt(lastIndex);
                    lastRenderer.Index = renderer.Index;

                    lastRenderer.MarkAllDirty();
                    lastRenderer.UpdateRendererData(out bool _);
                    MarkDataDirty();
                }

                _dynamicRenderers.FastRemoveByValue(renderer);
                --Size;
            }

            private void MarkDataDirty()
            {
                _isDataDirty = true;
            }

            private void ExpandData()
            {
                ToonBlobShadowsArrayUtils.ExpandArray(ref _data, DataAllocator, DefaultArrayOptions);
                InnerGroup.ExpandData();

                MarkDataDirty();
            }
        }
    }
}