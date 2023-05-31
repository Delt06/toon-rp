using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Extensions
{
    public readonly struct ToonRenderingExtensionContext
    {
        public readonly ScriptableRenderContext ScriptableRenderContext;
        public readonly Camera Camera;
        public readonly ToonCameraRendererSettings CameraRendererSettings;
        public readonly CullingResults CullingResults;
        public readonly ToonCameraRenderTarget CameraRenderTarget;

        public ToonRenderingExtensionContext(ScriptableRenderContext scriptableRenderContext, Camera camera,
            ToonCameraRendererSettings cameraRendererSettings, CullingResults cullingResults,
            ToonCameraRenderTarget cameraRenderTarget)
        {
            ScriptableRenderContext = scriptableRenderContext;
            Camera = camera;
            CameraRendererSettings = cameraRendererSettings;
            CullingResults = cullingResults;
            CameraRenderTarget = cameraRenderTarget;
        }
    }
}