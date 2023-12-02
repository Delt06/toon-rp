using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif // UNITY_EDITOR

namespace DELTation.ToonRP.Shadows.Blobs
{
    [ExecuteAlways]
    public sealed unsafe class ToonBlobShadowsManager : MonoBehaviour
    {
        private static readonly Dictionary<Scene, ToonBlobShadowsManager> Managers = new(new SceneEqualityComparer());
        public readonly List<ToonBlobShadowRenderer> DynamicRenderers = new();
        public readonly List<ToonBlobShadowRenderer> Renderers = new();
        private NativeArray<ToonBlobShadowsRendererData> _data;

        public ToonBlobShadowsRendererData* DataPtr => (ToonBlobShadowsRendererData*) _data.GetUnsafePtr();
        public NativeArray<ToonBlobShadowsRendererData> Data => _data;

        public static Dictionary<Scene, ToonBlobShadowsManager>.ValueCollection AllManagers => Managers.Values;

        private void Awake()
        {
            _data = new NativeArray<ToonBlobShadowsRendererData>(16, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory
            );
        }

        private void OnDestroy()
        {
            Renderers.Clear();
            DynamicRenderers.Clear();
            _data.Dispose();
            Managers.Remove(gameObject.scene);
        }

        [CanBeNull]
        public static ToonBlobShadowsManager GetManager(Camera camera) =>
            TryGetBlobShadowManager(camera.gameObject.scene, false, out ToonBlobShadowsManager manager) &&
            manager != null
                ? manager
                : null;

        private static bool TryGetBlobShadowManager(Scene scene, bool createIfNotFound,
            out ToonBlobShadowsManager manager)
        {
            manager = default;
            if (!scene.IsValid())
            {
                return false;
            }

            if (!Managers.TryGetValue(scene, out manager) && createIfNotFound)
            {
                if (scene.isLoaded)
                {
                    var gameObject = new GameObject("[Blob Shadows Manager]")
                    {
                        hideFlags = HideFlags.NotEditable | HideFlags.DontSave,
                    };
                    Managers[scene] = manager = gameObject.AddComponent<ToonBlobShadowsManager>();

                    SceneManager.MoveGameObjectToScene(gameObject, scene);
                }
            }

            return manager != null;
        }

        public static void OnRendererEnabled(ToonBlobShadowRenderer renderer)
        {
            if (IsPartOfPrefab(renderer))
            {
                return;
            }

            Scene scene = renderer.gameObject.scene;
            if (!TryGetBlobShadowManager(scene, true, out ToonBlobShadowsManager manager))
            {
                return;
            }

            manager.Renderers.Add(renderer);
            int index = manager.Renderers.Count - 1;

            if (index >= manager._data.Length)
            {
                ExpandArray(ref manager._data);
            }

            renderer.Init(manager, index);
            if (!renderer.IsStatic)
            {
                manager.DynamicRenderers.Add(renderer);
            }
        }

        private static void ExpandArray<T>(ref NativeArray<T> array) where T : struct
        {
            var newArray = new NativeArray<T>(array.Length * 2, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory
            );
            UnsafeUtility.MemCpy(newArray.GetUnsafePtr(), array.GetUnsafePtr(), UnsafeUtility.SizeOf<T>() * array.Length
            );
            array.Dispose();
            array = newArray;
        }

        public static void OnRendererDisabled(ToonBlobShadowRenderer renderer)
        {
            if (IsPartOfPrefab(renderer))
            {
                return;
            }

            Scene scene = renderer.gameObject.scene;
            if (!TryGetBlobShadowManager(scene, false, out ToonBlobShadowsManager manager))
            {
                return;
            }

            Assert.IsTrue(0 <= renderer.Index && renderer.Index < manager.Renderers.Count);

            int lastIndex = manager.Renderers.Count - 1;
            if (renderer.Index == lastIndex)
            {
                manager.Renderers.RemoveAt(renderer.Index);
            }
            else
            {
                // Swap with the last renderer and remove
                ToonBlobShadowRenderer lastRenderer = manager.Renderers[^1];
                manager.Renderers[renderer.Index] = lastRenderer;
                manager.Renderers.RemoveAt(lastIndex);
                lastRenderer.Index = renderer.Index;
                renderer.Shutdown();

                // Copy the moved renderer's data to the new index
                manager._data[lastRenderer.Index] = manager._data[lastIndex];
            }

            if (!renderer.IsStatic)
            {
                manager.DynamicRenderers.FastRemoveByValue(renderer);
            }
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

        private static bool IsPartOfPrefab(ToonBlobShadowRenderer renderer)
        {
#if UNITY_EDITOR
            PrefabStage currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            return currentPrefabStage != null && currentPrefabStage.IsPartOfPrefabContents(renderer.gameObject);
#else // !UNITY_EDITOR
            return false;
#endif // UNITY_EDITOR
        }

        private class SceneEqualityComparer : IEqualityComparer<Scene>
        {
            public bool Equals(Scene x, Scene y) => x.handle == y.handle;

            public int GetHashCode(Scene obj) => obj.handle;
        }
    }
}