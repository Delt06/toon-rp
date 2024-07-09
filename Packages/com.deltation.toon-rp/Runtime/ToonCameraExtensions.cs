using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.Assertions;

namespace DELTation.ToonRP
{
    /// <summary>
    /// Contains extension methods for Camera class. Extended from https://github.com/Unity-Technologies/Graphics/blob/master/Packages/com.unity.render-pipelines.universal/Runtime/UniversalAdditionalCameraData.cs
    /// </summary>
    public static class CameraExtensions
    {
        /// <summary>
        /// ToonRP exposes additional rendering data in a separate component.
        /// This method returns the additional data component for the given camera or create one if it doesn't exist yet.
        /// </summary>
        /// <param name="camera"></param>
        /// <returns>The <c>ToonAdditionalCameraData</c> for this camera.</returns>
        /// <see cref="ToonAdditionalCameraData"/>
        public static ToonAdditionalCameraData GetToonAdditionalCameraData(this Camera camera)
        {
            var gameObject = camera.gameObject;
            bool componentExists = gameObject.TryGetComponent<ToonAdditionalCameraData>(out var cameraData);
            if (!componentExists)
                cameraData = gameObject.AddComponent<ToonAdditionalCameraData>();

            return cameraData;
        }

        /// <summary>
        /// Returns the VolumeFrameworkUpdateMode set on the camera.
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static VolumeFrameworkUpdateMode GetVolumeFrameworkUpdateMode(this Camera camera)
        {
            ToonAdditionalCameraData cameraData = camera.GetToonAdditionalCameraData();
            return cameraData.VolumeFrameworkUpdateModeOption;
        }

        /// <summary>
        /// Sets the VolumeFrameworkUpdateMode for the camera.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="mode"></param>
        public static void SetVolumeFrameworkUpdateMode(this Camera camera, VolumeFrameworkUpdateMode mode)
        {
            ToonAdditionalCameraData cameraData = camera.GetToonAdditionalCameraData();
            if (cameraData.VolumeFrameworkUpdateModeOption == mode)
                return;

            bool requiredUpdatePreviously = cameraData.RequiresVolumeFrameworkUpdate;
            cameraData.VolumeFrameworkUpdateModeOption = mode;

            // We only update the local volume stacks for cameras set to ViaScripting.
            // Otherwise it will be updated in every frame.
            // We also check the previous value to make sure we're not updating when
            // switching between Camera ViaScripting and the ToonRP Asset set to ViaScripting
            if (requiredUpdatePreviously && !cameraData.RequiresVolumeFrameworkUpdate)
                camera.UpdateVolumeStack(cameraData);
        }

        /// <summary>
        /// Updates the volume stack for this camera.
        /// This function will only update the stack when the camera has VolumeFrameworkUpdateMode set to ViaScripting
        /// or when it set to UsePipelineSettings and the update mode on the Render Pipeline Asset is set to ViaScripting.
        /// </summary>
        /// <param name="camera"></param>
        public static void UpdateVolumeStack(this Camera camera)
        {
            ToonAdditionalCameraData cameraData = camera.GetToonAdditionalCameraData();
            camera.UpdateVolumeStack(cameraData);
        }

        /// <summary>
        /// Updates the volume stack for this camera.
        /// This function will only update the stack when the camera has ViaScripting selected or if
        /// the camera is set to UsePipelineSettings and the Render Pipeline Asset is set to ViaScripting.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="cameraData"></param>
        public static void UpdateVolumeStack(this Camera camera, ToonAdditionalCameraData cameraData)
        {
            Assert.IsNotNull(cameraData, "cameraData can not be null when updating the volume stack.");

            // We only update the local volume stacks for cameras set to ViaScripting.
            // Otherwise it will be updated in the frame.
            if (cameraData.RequiresVolumeFrameworkUpdate)
                return;

            // Create stack for camera
            if (cameraData.volumeStack == null)
                cameraData.GetOrCreateVolumeStack();

            camera.GetVolumeLayerMaskAndTrigger(cameraData, out LayerMask layerMask, out Transform trigger);
            VolumeManager.instance.Update(cameraData.volumeStack, trigger, layerMask);
        }

        /// <summary>
        /// Destroys the volume stack for this camera.
        /// </summary>
        /// <param name="camera"></param>
        public static void DestroyVolumeStack(this Camera camera)
        {
            ToonAdditionalCameraData cameraData = camera.GetToonAdditionalCameraData();
            camera.DestroyVolumeStack(cameraData);
        }

        /// <summary>
        /// Destroys the volume stack for this camera.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="cameraData"></param>
        public static void DestroyVolumeStack(this Camera camera, ToonAdditionalCameraData cameraData)
        {
            if (cameraData == null || cameraData.volumeStack == null)
                return;

            cameraData.volumeStack = null;
        }

        /// <summary>
        /// Returns the mask and trigger assigned for volumes on the camera.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="cameraData"></param>
        /// <param name="layerMask"></param>
        /// <param name="trigger"></param>
        internal static void GetVolumeLayerMaskAndTrigger(this Camera camera, ToonAdditionalCameraData cameraData, out LayerMask layerMask, out Transform trigger)
        {
            // Default values when there's no additional camera data available
            layerMask = 1; // "Default"
            trigger = camera.transform;

            if (cameraData != null)
            {
                layerMask = cameraData.VolumeLayerMask;
                trigger = (cameraData.VolumeTrigger != null) ? cameraData.VolumeTrigger : trigger;
            }
            else if (camera.cameraType == CameraType.SceneView)
            {
                // Try to mirror the MainCamera volume layer mask for the scene view - do not mirror the target
                var mainCamera = Camera.main;
                ToonAdditionalCameraData mainAdditionalCameraData = null;

                if (mainCamera != null && mainCamera.TryGetComponent(out mainAdditionalCameraData))
                {
                    layerMask = mainAdditionalCameraData.VolumeLayerMask;
                }

                trigger = (mainAdditionalCameraData != null && mainAdditionalCameraData.VolumeTrigger != null) ? mainAdditionalCameraData.VolumeTrigger : trigger;
            }
        }
    }

}
