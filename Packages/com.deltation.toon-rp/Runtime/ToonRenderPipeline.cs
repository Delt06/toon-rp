using DELTation.ToonRP.Extensions;
using DELTation.ToonRP.PostProcessing;
using DELTation.ToonRP.Shadows;
using DELTation.ToonRP.Xr;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public sealed class ToonRenderPipeline : RenderPipeline
    {
        public const string PipelineTag = "ToonRP";

        private readonly ToonCameraRenderer _cameraRenderer = new();
        private readonly ToonRampSettings _globalRampSettings;
        private readonly ToonShadowSettings _shadowSettings;
        private ToonCameraRendererSettings _cameraRendererSettings;
        private ToonRenderingExtensionSettings _extensions;
        private ToonPostProcessingSettings _postProcessingSettings;
        private VolumeProfile _defaultVolumeProfile;

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

            _defaultVolumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            LoadVolumeFrameworkDefaults(_defaultVolumeProfile);
            VolumeManager.instance.Initialize(globalDefaultVolumeProfile: _defaultVolumeProfile);

#if ENABLE_VR && ENABLE_XR_MODULE
            var occlusionMeshShader = Shader.Find(ToonXr.OcclusionMeshShaderName);
            var mirrorViewShader = Shader.Find(ToonXr.MirrorViewShaderName);
            XRSystem.Initialize(ToonXrPass.Create, occlusionMeshShader, mirrorViewShader);
#endif // ENABLE_VR && ENABLE_XR_MODULE
        }

        public ref ToonCameraRendererSettings CameraRendererSettings => ref _cameraRendererSettings;
        public ref ToonPostProcessingSettings PostProcessingSettings => ref _postProcessingSettings;
        public ref ToonRenderingExtensionSettings Extensions => ref _extensions;

        public void InvalidateExtensions() =>
            _cameraRenderer.InvalidateExtensions();

        public static Shader GetDefaultShader() => Shader.Find("Toon RP/Default");

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _cameraRenderer.Dispose();
            VolumeManager.instance.Deinitialize();

#if UNITY_EDITOR
            Object.DestroyImmediate(_defaultVolumeProfile);
#else
            Object.Destroy(_defaultVolumeProfile);
#endif
            
            XRSystem.Dispose();
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

            XRSystem.SetDisplayMSAASamples((MSAASamples) _cameraRendererSettings.Msaa);

            var sharedContext = new ToonRenderPipelineSharedContext();

#if UNITY_EDITOR
            // Update profile to match any changes the user makes in the Pass Asset
            LoadVolumeFrameworkDefaults(_defaultVolumeProfile);
            VolumeManager.instance.OnVolumeProfileChanged(_defaultVolumeProfile);
#endif

            foreach (Camera camera in cameras)
            {
                ToonAdditionalCameraData additionalCameraData = GetOrAddAdditionalCameraData(camera);

                // Prepare XR rendering
                bool xrActive = false;
                XRLayout xrLayout = XRSystem.NewLayout();
                bool enableXrRendering = EnableXrRendering(additionalCameraData, camera);
                xrLayout.AddCamera(camera, enableXrRendering);

                foreach ((Camera _, XRPass xrPass) in xrLayout.GetActivePasses())
                {
                    if (xrPass.enabled)
                    {
                        xrActive = true;
                        ToonXr.UpdateCameraStereoMatrices(camera, xrPass);

#if ENABLE_VR && ENABLE_XR_MODULE
                        additionalCameraData.XrPass = xrPass;
#endif // ENABLE_VR && ENABLE_XR_MODULE
                    }

                    UpdateVolumeFramework(camera, null);

                    _cameraRenderer.Render(context, ref sharedContext,
                        camera, additionalCameraData,
                        _cameraRendererSettings,
                        _globalRampSettings,
                        _shadowSettings,
                        _postProcessingSettings,
                        _extensions
                    );
                }

                if (xrActive)
                {
                    CommandBuffer cmd = CommandBufferPool.Get();
                    XRSystem.RenderMirrorView(cmd, camera);
                    context.ExecuteCommandBuffer(cmd);
                    context.Submit();
                    CommandBufferPool.Release(cmd);
                }

                XRSystem.EndLayout();
            }
        }

        private static bool EnableXrRendering(ToonAdditionalCameraData additionalCameraData, Camera camera)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            return additionalCameraData.EnableXRRendering && camera.targetTexture == null;
#else
            return false;
#endif // ENABLE_VR && ENABLE_XR_MODULE
        }

        private static ToonAdditionalCameraData GetOrAddAdditionalCameraData(Camera camera)
        {
            if (!camera.TryGetComponent(out ToonAdditionalCameraData additionalCameraData))
            {
                additionalCameraData = camera.gameObject.AddComponent<ToonAdditionalCameraData>();
            }

            return additionalCameraData;
        }


        /// <summary>
        /// Updates the volume framework for the given camera.
        /// </summary>
        /// <param name="camera">Camera to update</param>
        /// <param name="additionalCameraData">Camera data (with LayerMask and VolumeStack)</param>
        static void UpdateVolumeFramework(Camera camera, ToonAdditionalCameraData additionalCameraData)
        {
            // TO-DO: Name this scope properly intead of borrowing from the post-processing stack
            using var profScope = new ProfilingScope(NamedProfilingSampler.Get(ToonRpPassId.PostProcessingStack));

            // Update the volume framework for:
            // * All cameras in the editor when not in playmode
            // * scene cameras
            // * cameras with update mode set to EveryFrame
            
            bool shouldUpdate = camera.cameraType == CameraType.SceneView;
            shouldUpdate |= additionalCameraData != null && additionalCameraData.RequiresVolumeFrameworkUpdate;


#if UNITY_EDITOR
            shouldUpdate |= Application.isPlaying == false;
#endif


            // When we have volume updates per-frame disabled...
            if (!shouldUpdate && additionalCameraData)
            {
                // If an invalid volume stack is present, destroy it
                if (additionalCameraData.volumeStack != null && !additionalCameraData.volumeStack.isValid)
                {
                    camera.DestroyVolumeStack(additionalCameraData);
                }

                // Create a local volume stack and cache the state if it's null
                if (additionalCameraData.volumeStack == null)
                {
                    camera.UpdateVolumeStack(additionalCameraData);
                }

                VolumeManager.instance.stack = additionalCameraData.volumeStack;
                return;
            }

            // When we want to update the volumes every frame...

            // We destroy the volumeStack in the additional camera data, if present, to make sure
            // it gets recreated and initialized if the update mode gets later changed to ViaScripting...
            if (additionalCameraData && additionalCameraData.volumeStack != null)
            {
                camera.DestroyVolumeStack(additionalCameraData);
            }

            // Get the mask + trigger and update the stack
            camera.GetVolumeLayerMaskAndTrigger(additionalCameraData, out LayerMask layerMask, out Transform trigger);
            VolumeManager.instance.ResetMainStack();
            VolumeManager.instance.Update(trigger, layerMask);
        }


        /// <summary>
        /// Copies settings from all currently assigned passes into the volume profile.
        /// </summary>
        /// <param name="defaultProfile"></param>
        private void LoadVolumeFrameworkDefaults(VolumeProfile defaultProfile)
        {
            foreach (ToonPostProcessingPassAsset pass in _postProcessingSettings.Passes)
            {
                    pass.CopySettingsToVolumeProfile(defaultProfile);
            }
        }
    }
}