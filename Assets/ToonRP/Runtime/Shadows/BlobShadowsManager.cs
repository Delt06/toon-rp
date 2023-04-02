using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace ToonRP.Runtime.Shadows
{
    internal sealed class BlobShadowsManager : MonoBehaviour
    {
        [CanBeNull]
        private static BlobShadowsManager _instance;

        // private readonly List<BlobShadowRenderer> _renderersList = new();

        [NotNull]
        public static BlobShadowsManager Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                _instance = FindObjectOfType<BlobShadowsManager>();
                if (_instance != null)
                {
                    return _instance;
                }

                var gameObject = new GameObject
                {
                    name = "[Toon RP/Blob Shadows Manager]",
                };
                BlobShadowsManager instance = gameObject.AddComponent<BlobShadowsManager>();
                instance.OnCreated();
                return _instance = instance;
            }
        }

        public static HashSet<BlobShadowRenderer> Renderers { get; } = new();

        private void Awake()
        {
            OnCreated();
        }

        private void OnCreated()
        {
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(this);
            }

            // gameObject.hideFlags = HideFlags.HideAndDontSave;
            gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
        }

        public static void OnRendererEnabled(BlobShadowRenderer blobShadowRenderer)
        {
            Renderers.Add(blobShadowRenderer);
        }

        public static void OnRendererDisabled(BlobShadowRenderer blobShadowRenderer)
        {
            Renderers.Remove(blobShadowRenderer);
        }
    }
}