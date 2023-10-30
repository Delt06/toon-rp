using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Extensions
{
    public readonly struct ToonRenderingExtensionContext
    {
        public readonly ToonRenderingExtensionsCollection Collection;
        public readonly ScriptableRenderContext ScriptableRenderContext;
        public readonly Camera Camera;
        public readonly ToonCameraRendererSettings CameraRendererSettings;
        public readonly CullingResults CullingResults;
        public readonly ToonCameraRenderTarget CameraRenderTarget;
        public readonly ToonAdditionalCameraData AdditionalCameraData;

        public ToonRenderingExtensionContext(ToonRenderingExtensionsCollection collection,
            ScriptableRenderContext scriptableRenderContext, Camera camera,
            ToonCameraRendererSettings cameraRendererSettings, CullingResults cullingResults,
            ToonCameraRenderTarget cameraRenderTarget, ToonAdditionalCameraData additionalCameraData)
        {
            ScriptableRenderContext = scriptableRenderContext;
            Camera = camera;
            CameraRendererSettings = cameraRendererSettings;
            CullingResults = cullingResults;
            CameraRenderTarget = cameraRenderTarget;
            AdditionalCameraData = additionalCameraData;
            Collection = collection;
        }
    }
}