using UnityEngine;
using UnityEngine.Rendering;

namespace ToonRP.Runtime
{
    [CreateAssetMenu(menuName = "Rendering/Toon Render Pipeline")]
    public sealed class ToonRenderPipelineAsset : RenderPipelineAsset
    {
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
            MsaaResolveDepth = true,
            UseSrpBatching = true,
            UseDynamicBatching = false,
        };

        [ToonRpHeader("Shadows")]
        public ToonShadowSettings ShadowSettings = new()
        {
            MaxDistance = 100.0f,
            DistanceFade = 0.1f,
            HighQualityBlur = true,
            Directional =
            {
                Enabled = true, AtlasSize = ToonShadowSettings.TextureSize._1024, Threshold = 0.5f, Smoothness = 0.075f,
            },
        };

        public override Material defaultMaterial => new(defaultShader);

        public override Shader defaultShader => Shader.Find("Toon RP/Default");

        protected override RenderPipeline CreatePipeline() =>
            new ToonRenderPipeline(CameraRendererSettings, GlobalRampSettings, ShadowSettings);
    }
}