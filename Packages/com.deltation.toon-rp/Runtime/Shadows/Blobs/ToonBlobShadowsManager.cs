using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace DELTation.ToonRP.Shadows.Blobs
{
    [ExecuteAlways]
    public sealed unsafe class ToonBlobShadowsManager : MonoBehaviour
    {
        public readonly List<ToonBlobShadowRenderer> DynamicRenderers = new();
        public readonly List<ToonBlobShadowRenderer> Renderers = new();

        public NativeArray<ToonBlobShadowsRendererData> Data;

        public ToonBlobShadowsRendererData* DataPtr => (ToonBlobShadowsRendererData*) Data.GetUnsafePtr();

        private void Awake()
        {
            Data = new NativeArray<ToonBlobShadowsRendererData>(16, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory
            );
        }

        private void OnDestroy()
        {
            Renderers.Clear();
            DynamicRenderers.Clear();
            Data.Dispose();
            ToonBlobShadowsManagers.OnDestroyed(this);
        }

        public void ForceUpdateStaticStatus(ToonBlobShadowRenderer r)
        {
            DynamicRenderers.FastRemoveByValue(r);
            UpdateStaticStatus(r);
        }

        public void UpdateStaticStatus(ToonBlobShadowRenderer r)
        {
            if (r.IsStatic)
            {
                DynamicRenderers.FastRemoveByValue(r);
            }
            else
            {
                DynamicRenderers.Add(r);
            }
        }
    }
}