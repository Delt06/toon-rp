using DELTation.ToonRP.Extensions;
using DELTation.ToonRP.PostProcessing;
using DELTation.ToonRP.Shadows;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public sealed class ToonRenderPipeline : RenderPipeline
    {
        public const string PipelineTag = "ToonRP";

        private readonly ToonCameraRenderer _cameraRenderer = new();
        private readonly ToonCameraRendererSettings _cameraRendererSettings;
        private readonly ToonRenderingExtensionSettings _extensions;
        private readonly ToonRampSettings _globalRampSettings;
        private readonly ToonPostProcessingSettings _postProcessingSettings;
        private readonly ToonShadowSettings _shadowSettings;

        public ToonRenderPipeline(in ToonCameraRendererSettings cameraRendererSettings,
            in ToonRampSettings globalRampSettings, in ToonShadowSettings shadowSettings,
            in ToonPostProcessingSettings postProcessingSettings, ToonRenderingExtensionSettings extensions)
        {
            Shader.globalRenderPipeline = PipelineTag;

            _cameraRendererSettings = cameraRendererSettings;
            _globalRampSettings = globalRampSettings;
            _shadowSettings = shadowSettings;
            _postProcessingSettings = postProcessingSettings;
            _extensions = extensions;
            GraphicsSettings.useScriptableRenderPipelineBatching = _cameraRendererSettings.UseSrpBatching;
        }

        public static Shader GetDefaultShader() => Shader.Find("Toon RP/Default");

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _cameraRenderer.Dispose();
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            if (QualitySettings.activeColorSpace == ColorSpace.Gamma)
            {
                Debug.LogError(
                    "Toon RP does not support Gamma color space. Please switch to Linear color space in Project Settings > Player > Other Settings > Color Space"
                );
                return;
            }

            var sharedContext = new ToonRenderPipelineSharedContext();

            foreach (Camera camera in cameras)
            {
                ToonAdditionalCameraData additionalCameraData = GetOrAddAdditionalCameraData(camera);

                _cameraRenderer.Render(context, ref sharedContext,
                    camera, additionalCameraData,
                    _cameraRendererSettings,
                    _globalRampSettings,
                    _shadowSettings,
                    _postProcessingSettings,
                    _extensions
                );
            }
        }

        private static ToonAdditionalCameraData GetOrAddAdditionalCameraData(Camera camera)
        {
            if (!camera.TryGetComponent(out ToonAdditionalCameraData additionalCameraData))
            {
                additionalCameraData = camera.gameObject.AddComponent<ToonAdditionalCameraData>();
            }

            return additionalCameraData;
        }
    }
}