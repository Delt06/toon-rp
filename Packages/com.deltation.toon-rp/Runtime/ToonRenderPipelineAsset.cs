using System.Linq;
using DELTation.ToonRP.PostProcessing;
using DELTation.ToonRP.PostProcessing.BuiltIn;
using DELTation.ToonRP.Shadows;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    [CreateAssetMenu(menuName = "Rendering/Toon Render Pipeline Asset")]
    public sealed class ToonRenderPipelineAsset : RenderPipelineAsset
    {
        private static readonly string[] ForceIncludedShaderNames =
        {
            "Hidden/Toon RP/VSM Blur",
            "Hidden/Toon RP/Bloom",
            "Hidden/Toon RP/FXAA",
            "Hidden/Toon RP/Light Scattering",
            "Hidden/Toon RP/Outline (Inverted Hull)",
            "Hidden/Toon RP/SSAO",
            "Hidden/Toon RP/Blob Shadow Pass",
        };
        // Hold references to all shaders access in runtime to ensure they get included to the build
        [HideInInspector]
        public Shader[] ForceIncludedShaders;

        public ToonRampSettings GlobalRampSettings = new()
        {
            Threshold = 0.0f,
            SpecularThreshold = 0.99f,
            RimThreshold = 0.5f,
            Smoothness = 0.02f,
            SpecularSmoothness = 0.005f,
            RimSmoothness = 0.1f,
        };

        public ToonCameraRendererSettings CameraRendererSettings = new()
        {
            Msaa = ToonCameraRendererSettings.MsaaMode.Off,
            RenderScale = 1.0f,
            MaxRenderTextureHeight = 0,
            MaxRenderTextureWidth = 0,
            RenderTextureFilterMode = FilterMode.Bilinear,
            UseSrpBatching = true,
            UseDynamicBatching = false,
        };

        public ToonShadowSettings ShadowSettings = new()
        {
            Mode = ToonShadowSettings.ShadowMode.Vsm,
            Threshold = 0.5f, Smoothness = 0.075f,
            MaxDistance = 100.0f,
            DistanceFade = 0.1f,
            PatternScale = new Vector3(1, 0, 1),
            Vsm = new ToonVsmShadowSettings
            {
                Blur = ToonVsmShadowSettings.BlurMode.LowQuality,
                Directional =
                {
                    Enabled = true, AtlasSize = TextureSize._1024,
                    CascadeCount = 4,
                    CascadeRatio1 = 0.1f,
                    CascadeRatio2 = 0.25f,
                    CascadeRatio3 = 0.5f,
                },
            },
            Blobs = new ToonBlobShadowsSettings
            {
                Saturation = 1.0f,
                AtlasSize = TextureSize._128,
                Mode = BlobShadowsMode.Default,
            },
        };

        public ToonSsaoSettings Ssao = new()
        {
            Power = 10.0f,
            Radius = 0.1f,
            KernelSize = 4,
            Threshold = 0.6f,
            Smoothness = 0.2f,
            ResolutionFactor = 2,
            PatternScale = new Vector3(1, 0, 1),
        };

        public ToonPostProcessingSettings PostProcessing = new()
        {
            InvertedHullOutlines = new ToonInvertedHullOutlineSettings
            {
                Passes = new[]
                {
                    new ToonInvertedHullOutlineSettings.Pass
                    {
                        Name = "Outline",
                        Color = Color.black,
                        Thickness = 0.02f,
                        LayerMask = int.MaxValue,
                        MaxDistance = 0.0f,
                        DistanceFade = 0.1f,
                    },
                },
            },
        };

        public override Material defaultMaterial => new(defaultShader);

        public override Shader defaultShader => ToonRenderPipeline.GetDefaultShader();

        protected override void OnValidate()
        {
            base.OnValidate();

            if (ForceIncludedShaders == null || ForceIncludedShaders.Length != ForceIncludedShaderNames.Length)
            {
                ForceIncludedShaders = ForceIncludedShaderNames.Select(Shader.Find).ToArray();
            }
        }

        public ToonCameraRendererSettings.DepthPrePassMode GetEffectiveDepthPrePassMode() =>
            ToonCameraRenderer.GetOverrideDepthPrePassMode(CameraRendererSettings, PostProcessing, Ssao);

        protected override RenderPipeline CreatePipeline() =>
            new ToonRenderPipeline(CameraRendererSettings, GlobalRampSettings, ShadowSettings, PostProcessing, Ssao);
    }
}