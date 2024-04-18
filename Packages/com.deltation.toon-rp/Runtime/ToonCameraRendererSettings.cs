using System;
using DELTation.ToonRP.Attributes;
using DELTation.ToonRP.Lighting;
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
        [ToonRpShowIf(nameof(IsTiledLightingEnabled))]
        [Range(ToonTiledLighting.MinLightsPerTile, ToonTiledLighting.MaxLightsPerTile)]
        public int MaxLightsPerTile;

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
        public PrePassMode PrePass;
        public bool OpaqueTexture;
        public LayerMask OpaqueLayerMask;
        public LayerMask TransparentLayerMask;
        [Tooltip("Layer mask of objects, which will render zero motion vectors. Can be useful for selectively excluding objects from temporal effects")]
        public LayerMask MotionVectorsZeroLayerMask;

        public bool UseSrpBatching;
        public bool UseDynamicBatching;

        public bool ForceRenderToIntermediateBuffer;

        [Tooltip(
            "Use Vulkan/Metal render passes when possible. This feature is experimental."
        )]
        public bool NativeRenderPasses;

        [Tooltip(
            "Enable this to always use StoreAction.Store for camera depth. By default, it uses StoreAction.DontCare unless any extension or post-processing effect overrides it."
        )]
        public bool ForceStoreCameraDepth;
        public ToonRpBakedLightingFeatures BakedLightingFeatures;

        public bool IsTiledLightingEnabled =>
            AdditionalLights == AdditionalLightsMode.PerPixel &&
            TiledLighting;

        private bool PerPixelAdditionalLights => AdditionalLights == AdditionalLightsMode.PerPixel;
        private bool UseDefaultRenderTextureFormat => !OverrideRenderTextureFormat;
    }
}