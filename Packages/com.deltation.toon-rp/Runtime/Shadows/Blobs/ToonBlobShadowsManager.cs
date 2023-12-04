using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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

        public class Group : IDisposable
        {
            public readonly List<ToonBlobShadowRenderer> DynamicRenderers = new();
            public readonly List<ToonBlobShadowRenderer> Renderers = new();

            public NativeArray<ToonBlobShadowsRendererData> Data = new(16, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory
            );

            public ToonBlobShadowsRendererData* DataPtr => (ToonBlobShadowsRendererData*) Data.GetUnsafePtr();

            public void Dispose()
            {
                Renderers.Clear();
                DynamicRenderers.Clear();
                Data.Dispose();
            }

            public void ExpandData()
            {
                ExpandArray(ref Data);
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
        }
    }
}