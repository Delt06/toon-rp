#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using static DELTation.ToonRP.Shadows.Blobs.ToonBlobShadowsManager;

namespace DELTation.ToonRP.Shadows.Blobs
{
    internal static class ToonBlobShadowsManagers
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
                var gameObject = new GameObject("[Blob Shadows Manager]")
                {
                    hideFlags = HideFlags.NotEditable | HideFlags.DontSave | HideFlags.HideInHierarchy,
                };
                Managers[scene] = manager = gameObject.AddComponent<ToonBlobShadowsManager>();
                manager.Init();

                SceneManager.MoveGameObjectToScene(gameObject, scene);
            }

            if (manager != null)
            {
                manager.EnsureInitialized();
            }

            return manager != null && !manager.IsDestroyed;
        }

        public static void OnRendererEnabled(ToonBlobShadowRenderer renderer)
        {
            Scene scene = renderer.gameObject.scene;
            if (!TryGetBlobShadowManager(scene, true, out ToonBlobShadowsManager manager))
            {
                return;
            }

            Assert.IsTrue(renderer.Index == -1);

            Group group = manager.GetGroup(renderer.ShadowType);
            group.Renderers.Add(renderer);
            int index = group.Renderers.Count - 1;

            if (index >= group.Data.Length)
            {
                group.ExpandData();
            }

            renderer.AssignToManager(manager, index);
            if (!renderer.IsStatic)
            {
                group.DynamicRenderers.Add(renderer);
            }

            group.MarkDataDirty();
        }

        public static void OnRendererDisabled(ToonBlobShadowRenderer renderer)
        {
            Scene scene = renderer.gameObject.scene;
            if (!TryGetBlobShadowManager(scene, false, out ToonBlobShadowsManager manager))
            {
                return;
            }

            if (manager.TryGetGroup(renderer.AssignedGroupShadowType, out Group group))
            {
                Assert.IsTrue(0 <= renderer.Index && renderer.Index < group.Renderers.Count);

                int lastIndex = group.Renderers.Count - 1;
                if (renderer.Index == lastIndex)
                {
                    group.Renderers.RemoveAt(renderer.Index);
                }
                else
                {
                    // Swap with the last renderer and remove
                    ToonBlobShadowRenderer lastRenderer = group.Renderers[lastIndex];
                    group.Renderers[renderer.Index] = lastRenderer;
                    group.Renderers.RemoveAt(lastIndex);
                    lastRenderer.Index = renderer.Index;

                    lastRenderer.MarkAllDirty();
                    lastRenderer.UpdateRendererData(out bool _);
                    group.MarkDataDirty();
                }

                group.DynamicRenderers.FastRemoveByValue(renderer);
            }

            renderer.UnassignFromManager();
        }

        public static void OnDestroyed(ToonBlobShadowsManager manager)
        {
            Managers.Remove(manager.gameObject.scene);
        }

        private class SceneEqualityComparer : IEqualityComparer<Scene>
        {
            public bool Equals(Scene x, Scene y) => x.handle == y.handle;

            public int GetHashCode(Scene obj) => obj.handle;
        }

#if UNITY_EDITOR
        static ToonBlobShadowsManagers()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;
        }

        private static void BeforeAssemblyReload()
        {
            foreach (ToonBlobShadowsManager shadowsManager in All.ToArray())
            {
                shadowsManager.Destroy();
            }
        }
#endif // UNITY_EDITOR
    }
}