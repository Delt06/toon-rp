using ToonRP.Runtime.PostProcessing;
using UnityEngine;
using UnityEngine.Rendering;

namespace ToonRP.Runtime
{
    public sealed class ToonRenderPipeline : RenderPipeline
    {
        private readonly ToonCameraRenderer _cameraRenderer = new();
        private readonly ToonCameraRendererSettings _cameraRendererSettings;
        private readonly ToonRampSettings _globalRampSettings;
        private readonly ToonPostProcessingSettings _postProcessingSettings;
        private readonly ToonShadowSettings _shadowSettings;

        public ToonRenderPipeline(in ToonCameraRendererSettings cameraRendererSettings,
            in ToonRampSettings globalRampSettings, in ToonShadowSettings shadowSettings,
            in ToonPostProcessingSettings postProcessingSettings)
        {
            _cameraRendererSettings = cameraRendererSettings;
            _globalRampSettings = globalRampSettings;
            _shadowSettings = shadowSettings;
            _postProcessingSettings = postProcessingSettings;
            GraphicsSettings.useScriptableRenderPipelineBatching = _cameraRendererSettings.UseSrpBatching;
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (Camera camera in cameras)
            {
                _cameraRenderer.Render(context, camera,
                    _cameraRendererSettings,
                    _globalRampSettings,
                    _shadowSettings,
                    _postProcessingSettings
                );
            }
        }
    }
}