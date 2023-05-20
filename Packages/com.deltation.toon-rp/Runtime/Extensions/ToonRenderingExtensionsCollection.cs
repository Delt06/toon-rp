using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEngine;

namespace DELTation.ToonRP.Extensions
{
    public sealed class ToonRenderingExtensionsCollection
    {
        private static readonly int EventsCount;
        [ItemCanBeNull]
        private readonly List<IToonRenderingExtension>[] _extensions = new List<IToonRenderingExtension>[EventsCount];
        private readonly Dictionary<IToonRenderingExtension, ToonRenderingExtensionAsset> _sourceAssets = new();
        private ToonRenderingExtensionContext _context;

        private bool _initialized;

        static ToonRenderingExtensionsCollection() => EventsCount = Enum.GetValues(typeof(ToonRenderingEvent)).Length;

        public void Update(
            in ToonRenderingExtensionContext context,
            in ToonRenderingExtensionSettings settings)
        {
            CheckForReset();

            if (_initialized)
            {
                return;
            }

            if (context.Camera.cameraType < CameraType.SceneView)
            {
                return;
            }

            _sourceAssets.Clear();

            foreach (List<IToonRenderingExtension> extensions in _extensions)
            {
                extensions?.Clear();
            }

            if (settings.Extensions != null)
            {
                foreach (ToonRenderingExtensionAsset extensionAsset in settings.Extensions)
                {
                    if (extensionAsset == null)
                    {
                        continue;
                    }

                    IToonRenderingExtension renderingExtension = extensionAsset.CreateExtension();
                    AddExtension(extensionAsset.Event, renderingExtension);
                    _sourceAssets[renderingExtension] = extensionAsset;
                }
            }

            _initialized = true;
        }

        [Conditional("UNITY_EDITOR")]
        private void CheckForReset()
        {
            if (!_initialized)
            {
                return;
            }

            foreach (ToonRenderingExtensionAsset usedAsset in _sourceAssets.Values)
            {
                if (usedAsset != null)
                {
                    continue;
                }

                _initialized = false;
                return;
            }

            for (int index = 0; index < _extensions.Length; index++)
            {
                var @event = (ToonRenderingEvent) index;
                List<IToonRenderingExtension> extensions = _extensions[index];
                if (extensions == null)
                {
                    continue;
                }

                foreach (IToonRenderingExtension extension in extensions)
                {
                    if (_sourceAssets[extension].Event == @event)
                    {
                        continue;
                    }

                    _initialized = false;
                    return;
                }
            }
        }

        private void AddExtension(ToonRenderingEvent @event, IToonRenderingExtension extension)
        {
            List<IToonRenderingExtension> extensions = GetOrCreateExtensionList(@event);
            extensions.Add(extension);
        }

        public void RenderEvent(ToonRenderingEvent @event)
        {
            List<IToonRenderingExtension> extensions = GetExtensionListOrDefault(@event);
            if (extensions == null)
            {
                return;
            }

            foreach (IToonRenderingExtension extension in extensions)
            {
                extension.Render(_context);
            }
        }

        public void Setup(in ToonRenderingExtensionContext context)
        {
            _context = context;

            foreach (List<IToonRenderingExtension> extensions in _extensions)
            {
                if (extensions == null)
                {
                    continue;
                }

                foreach (IToonRenderingExtension extension in extensions)
                {
                    extension.Setup(_context);
                }
            }
        }

        public void Cleanup()
        {
            foreach (List<IToonRenderingExtension> extensions in _extensions)
            {
                if (extensions == null)
                {
                    continue;
                }

                foreach (IToonRenderingExtension extension in extensions)
                {
                    extension.Cleanup(_context);
                }
            }
        }

        [NotNull]
        private List<IToonRenderingExtension> GetOrCreateExtensionList(ToonRenderingEvent @event) =>
            _extensions[(int) @event] ??= new List<IToonRenderingExtension>();

        [CanBeNull]
        private List<IToonRenderingExtension> GetExtensionListOrDefault(ToonRenderingEvent @event) =>
            _extensions[(int) @event];
    }
}