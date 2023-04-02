using ToonRP.Runtime.PostProcessing;
using ToonRP.Runtime.Shadows;
using UnityEngine;
using UnityEngine.Rendering;

namespace ToonRP.Runtime
{
    [CreateAssetMenu(menuName = "Rendering/Toon Render Pipeline")]
    public sealed class ToonRenderPipelineAsset : RenderPipelineAsset
    {
        // Hold references to all shaders access in runtime to ensure they get included to the build
        [HideInInspector]
        public Shader[] ForceIncludedShaders;

        public ToonRampSettings GlobalRampSettings = new()
        {
            Threshold = 0.0f,
            SpecularThreshold = 0.99f,
            Smoothness = 0.02f,
            SpecularSmoothness = 0.005f,
        };

        public ToonCameraRendererSettings CameraRendererSettings = new()
        {
            Msaa = ToonCameraRendererSettings.MsaaMode.Off,
            UseSrpBatching = true,
            UseDynamicBatching = false,
        };

        public ToonShadowSettings ShadowSettings = new()
        {
            Mode = ToonShadowSettings.ShadowMode.Vsm,
            Threshold = 0.5f, Smoothness = 0.075f,
            Vsm = new ToonVsmShadowSettings
            {
                MaxDistance = 100.0f,
                DistanceFade = 0.1f,
                HighQualityBlur = true,
                Directional =
                {
                    Enabled = true, AtlasSize = TextureSize._1024,
                },
            },
            Blobs = new ToonBlobShadowsSettings
            {
                GPUInstancing = true,
                Saturation = 1.0f,
                AtlasSize = TextureSize._1024,
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
            Pattern = new ToonSsaoSettings.PatternSettings
            {
                Enabled = true,
                Scale = Vector3.one * 15.0f,
                Thickness = 0.5f,
                Smoothness = 0.1f,
                MaxDistance = 15.0f,
                DistanceFade = 0.5f,
            },
        };

        public ToonPostProcessingSettings PostProcessing = new()
        {
            Bloom = new ToonBloomSettings
            {
                Intensity = 1.0f,
                Threshold = 0.95f,
                ThresholdKnee = 0.5f,
                MaxIterations = ToonBloom.MaxIterations,
                DownsampleLimit = 2,
                Pattern = new ToonBloomSettings.PatternSettings
                {
                    Scale = 350,
                    Power = 2,
                    Multiplier = 4,
                    Smoothness = 0.5f,
                },
            },
            Outline = new ToonOutlineSettings
            {
                Mode = ToonOutlineSettings.OutlineMode.Off,
                InvertedHull = new ToonInvertedHullOutlineSettings
                {
                    Passes = new[]
                    {
                        new ToonInvertedHullOutlineSettings.Pass
                        {
                            Name = "Outline",
                            Color = Color.black,
                            Thickness = 0.02f,
                            LayerMask = int.MaxValue,
                            MaxDistance = 100.0f,
                            DistanceFade = 0.1f,
                        },
                    },
                },
            },
        };

        public override Material defaultMaterial => new(defaultShader);

        public override Shader defaultShader => Shader.Find("Toon RP/Default");

        protected override RenderPipeline CreatePipeline() =>
            new ToonRenderPipeline(CameraRendererSettings, GlobalRampSettings, ShadowSettings, PostProcessing, Ssao);
    }
}