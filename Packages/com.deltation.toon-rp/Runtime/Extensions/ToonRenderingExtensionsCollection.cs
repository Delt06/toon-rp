using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Extensions
{
    public sealed class ToonRenderingExtensionsCollection : IToonRenderingExtensionSettingsStorage
    {
        public delegate bool ExtensionPredicate([NotNull] IToonRenderingExtension extension,
            in ToonRenderingExtensionContext context);

        private readonly List<EventExtensionsList> _extensions = new();
        private readonly List<EventExtensionsList> _filteredExtensions = new();
        private readonly Dictionary<IToonRenderingExtension, ToonRenderingExtensionAsset> _sourceAssets = new();
        private ToonRenderingExtensionContext _context;

        private bool _initialized;

        public TSettings GetSettings<TSettings>(IToonRenderingExtension extension)
        {
            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            if (!_sourceAssets.TryGetValue(extension, out ToonRenderingExtensionAsset sourceAsset))
            {
                throw new ArgumentException(
                    $"The provided extension of type {extension.GetType()} is not part of this settings provider.",
                    nameof(extension)
                );
            }

            if (sourceAsset is not ToonRenderingExtensionAsset<TSettings> castedSourceAsset)
            {
                throw new ArgumentException(
                    $"The provided extension of type {extension.GetType()} is linked to an asset, but it does not store settings of type {typeof(TSettings)}.",
                    nameof(extension)
                );
            }

            return castedSourceAsset.Settings;
        }

        public void Dispose()
        {
            foreach (EventExtensionsList extensionList in _extensions)
            {
                if (extensionList.Extensions == null)
                {
                    continue;
                }

                foreach (IToonRenderingExtension extension in extensionList.Extensions)
                {
                    extension?.Dispose();
                }
            }
        }

        public void PreSetup(in ToonRenderingExtensionSettings settings)
        {
            CheckForReset();

            if (_initialized)
            {
                return;
            }

            _sourceAssets.Clear();

            foreach (EventExtensionsList extensionsList in _extensions)
            {
                extensionsList.Extensions?.Clear();
            }

            if (settings.Extensions != null)
            {
                foreach (ToonRenderingExtensionAsset extensionAsset in settings.Extensions)
                {
                    if (extensionAsset == null)
                    {
                        continue;
                    }

                    foreach (ToonRenderingEvent renderingEvent in ToonRenderingEvents.All)
                    {
                        IToonRenderingExtension renderingExtension =
                            extensionAsset.CreateExtensionOrDefault(renderingEvent);
                        if (renderingExtension != null)
                        {
                            AddExtension(renderingEvent, renderingExtension);
                            _sourceAssets[renderingExtension] = extensionAsset;
                        }
                    }
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

            for (int index = 0; index < _extensions.Count; index++)
            {
                var @event = (ToonRenderingEvent) index;
                EventExtensionsList extensionsList = _extensions[index];
                if (extensionsList.Extensions == null)
                {
                    continue;
                }

                foreach (IToonRenderingExtension extension in extensionsList.Extensions)
                {
                    if (_sourceAssets[extension].UsesRenderingEvent(@event))
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
            EventExtensionsList extensionsList = GetOrCreateExtensionList(@event);
            extensionsList.Extensions.Add(extension);
        }

        public void RenderEvent(ToonRenderingEvent @event)
        {
            if (TryGetFilteredExtensionsList(@event, out EventExtensionsList extensionsList))
            {
                foreach (IToonRenderingExtension extension in extensionsList.Extensions)
                {
                    extension.Render();
                }
            }
        }

        public void Setup(in ToonRenderingExtensionContext context)
        {
            _context = context;

            foreach (EventExtensionsList extensionsList in _filteredExtensions)
            {
                extensionsList.Extensions?.Clear();
            }

            foreach (EventExtensionsList extensionsList in _extensions)
            {
                if (extensionsList.Extensions == null)
                {
                    continue;
                }

                foreach (IToonRenderingExtension extension in extensionsList.Extensions)
                {
                    if (!extension.ShouldRender(_context))
                    {
                        continue;
                    }

                    extension.Setup(_context, this);
                    AddToFilteredExtensionsList(extensionsList.Event, extension);
                }
            }
        }

        private void AddToFilteredExtensionsList(ToonRenderingEvent renderingEvent, IToonRenderingExtension extension)
        {
            if (TryGetFilteredExtensionsList(renderingEvent, out EventExtensionsList extensionsList))
            {
                extensionsList.Extensions.Add(extension);
            }
            else
            {
                _filteredExtensions.Add(new EventExtensionsList
                    {
                        Event = renderingEvent,
                        Extensions = new List<IToonRenderingExtension> { extension },
                    }
                );
            }
        }

        private bool TryGetFilteredExtensionsList(ToonRenderingEvent renderingEvent,
            out EventExtensionsList extensionsList)
        {
            foreach (EventExtensionsList list in _filteredExtensions)
            {
                if (list.Event == renderingEvent)
                {
                    extensionsList = list;
                    return true;
                }
            }

            extensionsList = default;
            return false;
        }

        public void Cleanup()
        {
            foreach (EventExtensionsList extensionsList in _filteredExtensions)
            {
                if (extensionsList.Extensions == null)
                {
                    continue;
                }

                foreach (IToonRenderingExtension extension in extensionsList.Extensions)
                {
                    extension.Cleanup();
                }
            }
        }

        public void OnPrePass(PrePassMode prePassMode, ref ScriptableRenderContext context,
            CommandBuffer cmd,
            ref DrawingSettings drawingSettings, ref FilteringSettings filteringSettings,
            ref RenderStateBlock renderStateBlock)
        {
            foreach (EventExtensionsList extensionsList in _filteredExtensions)
            {
                if (extensionsList.Extensions == null)
                {
                    continue;
                }

                foreach (IToonRenderingExtension extension in extensionsList.Extensions)
                {
                    extension.OnPrePass(prePassMode,
                        ref context, cmd,
                        ref drawingSettings, ref filteringSettings, ref renderStateBlock
                    );
                }
            }
        }

        public bool TrueForAny(ExtensionPredicate predicate)
        {
            foreach (EventExtensionsList extensionsList in _filteredExtensions)
            {
                if (extensionsList.Extensions == null)
                {
                    continue;
                }

                foreach (IToonRenderingExtension extension in extensionsList.Extensions)
                {
                    if (predicate(extension, _context))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private EventExtensionsList GetOrCreateExtensionList(ToonRenderingEvent @event)
        {
            foreach (EventExtensionsList extensionsList in _extensions)
            {
                if (extensionsList.Event == @event)
                {
                    return extensionsList;
                }
            }

            var newExtensionsList = new EventExtensionsList
            {
                Event = @event,
                Extensions = new List<IToonRenderingExtension>(),
            };
            _extensions.Add(newExtensionsList);
            return newExtensionsList;
        }

        public void Invalidate()
        {
            _initialized = false;
        }

        private struct EventExtensionsList
        {
            [CanBeNull]
            public List<IToonRenderingExtension> Extensions;
            public ToonRenderingEvent Event;
        }
    }
}