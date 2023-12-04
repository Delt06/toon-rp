using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace DELTation.ToonRP.Shadows.Blobs
{
    internal static unsafe class ToonBlobShadowsManagers
    {
        private static readonly Dictionary<Scene, ToonBlobShadowsManager> Managers = new(new SceneEqualityComparer());
        public static Dictionary<Scene, ToonBlobShadowsManager>.ValueCollection All => Managers.Values;

        [CanBeNull]
        public static ToonBlobShadowsManager Get(Camera camera) =>
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
                        hideFlags = HideFlags.NotEditable | HideFlags.DontSave | HideFlags.HideInHierarchy,
                    };
                    Managers[scene] = manager = gameObject.AddComponent<ToonBlobShadowsManager>();

                    SceneManager.MoveGameObjectToScene(gameObject, scene);
                }
            }

            return manager != null;
        }

        public static void OnRendererEnabled(ToonBlobShadowRenderer renderer)
        {
            Scene scene = renderer.gameObject.scene;
            if (!TryGetBlobShadowManager(scene, true, out ToonBlobShadowsManager manager))
            {
                return;
            }

            manager.Renderers.Add(renderer);
            int index = manager.Renderers.Count - 1;

            if (index >= manager.Data.Length)
            {
                ExpandArray(ref manager.Data);
            }

            renderer.Init(manager, index);
            if (!renderer.IsStatic)
            {
                manager.DynamicRenderers.Add(renderer);
            }
        }

        public static void OnRendererDisabled(ToonBlobShadowRenderer renderer)
        {
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
                manager.Data[lastRenderer.Index] = manager.Data[lastIndex];
            }

            if (!renderer.IsStatic)
            {
                manager.DynamicRenderers.FastRemoveByValue(renderer);
            }
        }

        public static void OnDestroyed(ToonBlobShadowsManager manager)
        {
            Managers.Remove(manager.gameObject.scene);
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

        private class SceneEqualityComparer : IEqualityComparer<Scene>
        {
            public bool Equals(Scene x, Scene y) => x.handle == y.handle;

            public int GetHashCode(Scene obj) => obj.handle;
        }
    }
}