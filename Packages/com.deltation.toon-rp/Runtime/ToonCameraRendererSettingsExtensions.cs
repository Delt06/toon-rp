using UnityEngine;

namespace DELTation.ToonRP
{
    public static class ToonCameraRendererSettingsExtensions
    {
        public static bool IsTiledLightingEnabledAndSupported(in this ToonCameraRendererSettings settings) =>
            settings.IsTiledLightingEnabled &&
            SystemInfo.supportsComputeShaders;
    }
}