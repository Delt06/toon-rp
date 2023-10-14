using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    public static class ToonTemporalAAUtils
    {
        public static bool CameraSupportsTaa(Camera camera) => camera.cameraType == CameraType.Game;

        public static Matrix4x4 CalculateJitterMatrix(
            in ToonPostProcessingSettings postProcessingSettings,
            Camera camera,
            ToonCameraRenderTarget renderTarget)
        {
            Matrix4x4 jitterMatrix = Matrix4x4.identity;

            if (!CameraSupportsTaa(camera))
            {
                return jitterMatrix;
            }

            if (!postProcessingSettings.Enabled)
            {
                return jitterMatrix;
            }

            if (!TryGetTaaSettings(postProcessingSettings, out ToonTemporalAASettings taaSettings))
            {
                return jitterMatrix;
            }

            int taaFrameIndex = Time.frameCount;

            float actualWidth = renderTarget.Width;
            float actualHeight = renderTarget.Height;
            float jitterScale = taaSettings.JitterScale;

            Vector2 jitter = CalculateJitter(taaFrameIndex) * jitterScale;

            float offsetX = jitter.x * (2.0f / actualWidth);
            float offsetY = jitter.y * (2.0f / actualHeight);

            jitterMatrix = Matrix4x4.Translate(new Vector3(offsetX, offsetY, 0.0f));

            return jitterMatrix;
        }

        private static Vector2 CalculateJitter(int frameIndex)
        {
            // The variance between 0 and the actual halton sequence values reveals noticeable
            // instability in Unity's shadow maps, so we avoid index 0.
            float jitterX = HaltonSequence.Get((frameIndex & 1023) + 1, 2) - 0.5f;
            float jitterY = HaltonSequence.Get((frameIndex & 1023) + 1, 3) - 0.5f;

            return new Vector2(jitterX, jitterY);
        }

        private static bool TryGetTaaSettings(in ToonPostProcessingSettings postProcessingSettings,
            out ToonTemporalAASettings taaSettings)
        {
            foreach (ToonPostProcessingPassAsset passAsset in postProcessingSettings.Passes)
            {
                if (passAsset is ToonPostProcessingPassAsset<ToonTemporalAASettings> taaAsset)
                {
                    taaSettings = taaAsset.Settings;
                    return true;
                }
            }

            taaSettings = default;
            return false;
        }
    }
}