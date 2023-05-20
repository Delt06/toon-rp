using DELTation.ToonRP.Extensions;
using DELTation.ToonRP.PostProcessing;
using DELTation.ToonRP.PostProcessing.BuiltIn;
using DELTation.ToonRP.Shadows;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public sealed class ToonRenderPipeline : RenderPipeline
    {
        private readonly ToonCameraRenderer _cameraRenderer = new();
        private readonly ToonCameraRendererSettings _cameraRendererSettings;
        private readonly ToonRenderingExtensionSettings _extensions;
        private readonly ToonRampSettings _globalRampSettings;
        private readonly ToonPostProcessingSettings _postProcessingSettings;
        private readonly ToonShadowSettings _shadowSettings;
        private readonly ToonSsaoSettings _ssaoSettings;

        public ToonRenderPipeline(in ToonCameraRendererSettings cameraRendererSettings,
            in ToonRampSettings globalRampSettings, in ToonShadowSettings shadowSettings,
            in ToonPostProcessingSettings postProcessingSettings, ToonRenderingExtensionSettings extensions,
            in ToonSsaoSettings ssaoSettings)
        {
            _cameraRendererSettings = cameraRendererSettings;
            _globalRampSettings = globalRampSettings;
            _shadowSettings = shadowSettings;
            _postProcessingSettings = postProcessingSettings;
            _extensions = extensions;
            _ssaoSettings = ssaoSettings;
            GraphicsSettings.useScriptableRenderPipelineBatching = _cameraRendererSettings.UseSrpBatching;
        }

        public static Shader GetDefaultShader() => Shader.Find("Toon RP/Default");

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (Camera camera in cameras)
            {
                _cameraRenderer.Render(context, camera,
                    _cameraRendererSettings,
                    _globalRampSettings,
                    _shadowSettings,
                    _postProcessingSettings,
                    _extensions,
                    _ssaoSettings
                );
            }
        }
    }
}