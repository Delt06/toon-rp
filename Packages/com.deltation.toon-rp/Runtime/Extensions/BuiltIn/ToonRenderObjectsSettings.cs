using System;
using DELTation.ToonRP.Attributes;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    [Serializable]
    public struct ToonRenderObjectsSettings
    {
        public ToonRenderingEvent Event;
        public string PassName;
        public bool ClearDepth;
        public bool ClearStencil;
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
            public DepthOverrideSettings Depth;
            public StencilOverrideSettings Stencil;
            public ToonCameraOverrideSettings Camera;

            [Serializable]
            public struct DepthOverrideSettings
            {
                public bool Enabled;
                [ToonRpShowIf(nameof(Enabled))]
                public bool WriteDepth;
                [ToonRpShowIf(nameof(Enabled))]
                public CompareFunction DepthTest;
            }

            [Serializable]
            public struct StencilOverrideSettings
            {
                public bool Enabled;
                [ToonRpShowIf(nameof(Enabled))] [Range(0, byte.MaxValue)]
                public byte Value;
                [ToonRpShowIf(nameof(Enabled))] [Range(0, byte.MaxValue)]
                public byte ReadMask;
                [ToonRpShowIf(nameof(Enabled))] [Range(0, byte.MaxValue)]
                public byte WriteMask;
                [ToonRpShowIf(nameof(Enabled))]
                public CompareFunction CompareFunction;
                [ToonRpShowIf(nameof(Enabled))]
                public StencilOp Pass;
                [ToonRpShowIf(nameof(Enabled))]
                public StencilOp Fail;
                [ToonRpShowIf(nameof(Enabled))]
                public StencilOp ZFail;
            }
        }
    }
}