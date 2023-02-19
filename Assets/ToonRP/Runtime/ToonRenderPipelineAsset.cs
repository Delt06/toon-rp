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

        [ToonRpHeader("Camera Renderer")]
        public ToonCameraRendererSettings CameraRendererSettings = new()
        {
            UseSrpBatching = true,
            UseDynamicBatching = false,
        };

        public override Material defaultMaterial => new(defaultShader);

        public override Shader defaultShader => Shader.Find("Toon RP/Default");

        protected override RenderPipeline CreatePipeline() =>
            new ToonRenderPipeline(CameraRendererSettings, GlobalRampSettings);
    }
}