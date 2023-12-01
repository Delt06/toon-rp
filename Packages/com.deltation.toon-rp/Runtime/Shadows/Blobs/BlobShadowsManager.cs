using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace DELTation.ToonRP.Shadows.Blobs
{
    [ExecuteAlways]
    public sealed class BlobShadowsManager : MonoBehaviour
    {
        private static readonly Dictionary<Scene, BlobShadowsManager> Managers = new();
        public readonly List<BlobShadowRenderer> Renderers = new();

        public static Dictionary<Scene, BlobShadowsManager>.ValueCollection AllManagers => Managers.Values;

        private void OnDestroy()
        {
            Renderers.Clear();
            Managers.Remove(gameObject.scene);
        }

        [CanBeNull]
        public static List<BlobShadowRenderer> GetRenderers(Camera camera) =>
            TryGetBlobShadowManager(camera.gameObject.scene, false, out BlobShadowsManager manager) && manager != null
                ? manager.Renderers
                : null;

        private static bool TryGetBlobShadowManager(Scene scene, bool createIfNotFound, out BlobShadowsManager manager)
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
                    Managers[scene] = manager = gameObject.AddComponent<BlobShadowsManager>();

                    SceneManager.MoveGameObjectToScene(gameObject, scene);
                }
            }

            return manager != null;
        }

        public static void OnRendererEnabled(BlobShadowRenderer renderer)
        {
            Scene scene = renderer.gameObject.scene;
            if (!TryGetBlobShadowManager(scene, true, out BlobShadowsManager manager))
            {
                return;
            }

            Assert.IsTrue(renderer.Index == -1);

            manager.Renderers.Add(renderer);
            renderer.Index = manager.Renderers.Count - 1;
        }

        public static void OnRendererDisabled(BlobShadowRenderer renderer)
        {
            Scene scene = renderer.gameObject.scene;
            if (!TryGetBlobShadowManager(scene, false, out BlobShadowsManager manager))
            {
                return;
            }

            Assert.IsTrue(0 <= renderer.Index && renderer.Index < manager.Renderers.Count);

            // Swap with the last renderer and remove
            BlobShadowRenderer lastRenderer = manager.Renderers[^1];
            manager.Renderers[renderer.Index] = lastRenderer;
            manager.Renderers.RemoveAt(manager.Renderers.Count - 1);
            lastRenderer.Index = renderer.Index;
            renderer.Index = -1;
        }
    }
}