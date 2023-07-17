using System;
using UnityEngine;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    [Serializable]
    public struct ToonRenderObjectsSettings
    {
        public ToonRenderingEvent Event;
        public string PassName;
        public FilterSettings Filters;
        public OverrideSettings Overrides;

        [Serializable]
        public struct FilterSettings
        {
            public enum RenderQueue
            {
                Opaque,
                Transparent,
            }

            public RenderQueue Queue;
            public LayerMask LayerMask;
            public string[] LightModeTags;
        }

        [Serializable]
        public struct OverrideSettings
        {
            public Material Material;
            public ToonCameraOverrideSettings Camera;
        }
    }
}