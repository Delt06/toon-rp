using System;
using DELTation.ToonRP.Attributes;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace DELTation.ToonRP
{
    [Serializable]
    public struct ToonCameraRendererSettings
    {
        public enum AdditionalLightsMode
        {
            Off,
            PerPixel,
            PerVertex,
        }

        public enum MsaaMode
        {
            Off = 1,
            _2x = 2,
            _4x = 4,
            _8x = 8,
        }

        public AdditionalLightsMode AdditionalLights;
        [ToonRpShowIf(nameof(PerPixelAdditionalLights))]
        public bool TiledLighting;

        public bool OverrideRenderTextureFormat;

        [ToonRpShowIf(nameof(OverrideRenderTextureFormat))]
        public GraphicsFormat RenderTextureFormat;

        [ToonRpShowIf(nameof(UseDefaultRenderTextureFormat))]
        public bool AllowHdr;

        public bool Stencil;
        public MsaaMode Msaa;
        [Range(0.25f, 2.0f)]
        public float RenderScale;
        [Min(0)]
        public int MaxRenderTextureWidth;
        [Min(0)]
        public int MaxRenderTextureHeight;
        public FilterMode RenderTextureFilterMode;
        public DepthPrePassMode DepthPrePass;
        public LayerMask OpaqueLayerMask;
        public LayerMask TransparentLayerMask;

        public bool UseSrpBatching;
        public bool UseDynamicBatching;

        public bool IsTiledLightingEnabledAndSupported =>
            IsTiledLightingEnabled &&
            SystemInfo.supportsComputeShaders;

        private bool IsTiledLightingEnabled =>
            AdditionalLights == AdditionalLightsMode.PerPixel &&
            TiledLighting;

        private bool PerPixelAdditionalLights => AdditionalLights == AdditionalLightsMode.PerPixel;
        private bool UseDefaultRenderTextureFormat => !OverrideRenderTextureFormat;
    }
}