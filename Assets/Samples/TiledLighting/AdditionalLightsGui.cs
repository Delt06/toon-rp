using DELTation.ToonRP;
using UnityEngine;
using UnityEngine.Rendering;

namespace Samples.TiledLighting
{
    public class AdditionalLightsGui : MonoBehaviour
    {
        public float DesignWidth = 640.0f;

        private void OnGUI()
        {
            RenderPipeline currentPipeline = RenderPipelineManager.currentPipeline;
            if (currentPipeline is not ToonRenderPipeline toonRenderPipeline)
            {
                return;
            }

            float resX = Screen.width / DesignWidth;
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(resX, resX, 1.0f));

            ref ToonCameraRendererSettings cameraRendererSettings = ref toonRenderPipeline.CameraRendererSettings;
            bool oldAdditionalLights = cameraRendererSettings.AdditionalLights !=
                                       ToonCameraRendererSettings.AdditionalLightsMode.Off;
            bool newAdditionalLights = GUILayout.Toggle(oldAdditionalLights, "Additional Lights");

            if (oldAdditionalLights != newAdditionalLights)
            {
                cameraRendererSettings.AdditionalLights = newAdditionalLights
                    ? ToonCameraRendererSettings.AdditionalLightsMode.PerPixel
                    : ToonCameraRendererSettings.AdditionalLightsMode.Off;
            }

            if (oldAdditionalLights)
            {
                bool oldTiledLighting = cameraRendererSettings.TiledLighting;
                bool newTiledLighting = GUILayout.Toggle(oldTiledLighting, "Tiled Lighting");
                if (oldTiledLighting != newTiledLighting)
                {
                    cameraRendererSettings.TiledLighting = newTiledLighting;
                }
            }

            GUI.matrix = oldMatrix;
        }
    }
}