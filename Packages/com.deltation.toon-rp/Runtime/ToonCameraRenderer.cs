using System;
using System.Collections.Generic;
using DELTation.ToonRP.Extensions;
using DELTation.ToonRP.Lighting;
using DELTation.ToonRP.PostProcessing;
using DELTation.ToonRP.PostProcessing.BuiltIn;
using DELTation.ToonRP.Shadows;
using DELTation.ToonRP.Xr;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using static DELTation.ToonRP.ToonCameraRendererSettings;

namespace DELTation.ToonRP
{
    public sealed partial class ToonCameraRenderer : IDisposable
    {
        private const string DefaultCmdName = "Render Camera";
        public static readonly ShaderTagId[] ShaderTagIds =
        {
            new(ToonPasses.Forward.LightMode),
            new("SRPDefaultUnlit"),
        };
        private static readonly int TimeParametersId = Shader.PropertyToID("_TimeParameters");
        private readonly ToonDepthPrePass _depthPrePass;
        private readonly ToonRenderingExtensionsCollection _extensionsCollection = new();
        private readonly CommandBuffer _finalBlitCmd = new() { name = "Final Blit" };
        private readonly ToonGlobalRamp _globalRamp = new();
        private readonly ToonLighting _lighting = new();
        private readonly ToonMotionVectorsPrePass _motionVectorsPrePass;
        private readonly ToonOpaqueTexture _opaqueTexture;
        private readonly ToonPostProcessing _postProcessing = new();

        private readonly ToonCameraRenderTarget _renderTarget = new();
        private readonly ToonShadows _shadows = new();
        private readonly ToonTiledLighting _tiledLighting;
        private ToonAdditionalCameraData _additionalCameraData;

        private Camera _camera;
        private ToonCameraData _cameraData;

        private string _cmdName = DefaultCmdName;
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;
        private ToonRenderingExtensionContext _extensionContext;
        private PrePassMode _prePassMode;
        private bool _requireStencil;
        private ToonCameraRendererSettings _settings;

        public ToonCameraRenderer()
        {
            _tiledLighting = new ToonTiledLighting(_lighting);
            _opaqueTexture = new ToonOpaqueTexture(_renderTarget);
            _depthPrePass = new ToonDepthPrePass();
            _motionVectorsPrePass = new ToonMotionVectorsPrePass(_depthPrePass);
        }

        public void Dispose()
        {
            _renderTarget.Dispose();
            _shadows.Dispose();
            _tiledLighting?.Dispose();
            _extensionsCollection.Dispose();
            _postProcessing.Dispose();
        }

        private static GraphicsFormat GetDefaultGraphicsFormat() =>
            GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default, true);

        [Pure]
        public static GraphicsFormat GetRenderTextureColorFormat(in ToonCameraRendererSettings settings,
            bool ignoreMsaa = false)
        {
            if (!settings.OverrideRenderTextureFormat)
            {
                return settings.AllowHdr
                    ? GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.DefaultHDR, false)
                    : GetDefaultGraphicsFormat();
            }

            GraphicsFormat rawGraphicsFormat = settings.RenderTextureFormat;
            FormatUsage formatUsage = FormatUsage.Render;
            if (!ignoreMsaa)
            {
                // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                formatUsage |= settings.Msaa switch
                {
                    MsaaMode._2x => FormatUsage.MSAA2x,
                    MsaaMode._4x => FormatUsage.MSAA4x,
                    MsaaMode._8x => FormatUsage.MSAA8x,
                    _ => 0,
                };
            }

            return SystemInfo.GetCompatibleFormat(rawGraphicsFormat, formatUsage);
        }

        public static PrePassMode GetOverridePrePassMode(in ToonCameraRendererSettings settings,
            in ToonPostProcessingSettings postProcessingSettings,
            in ToonRenderingExtensionSettings extensionSettings)
        {
            PrePassMode mode = settings.PrePass;

            if (postProcessingSettings.Passes != null)
            {
                foreach (ToonPostProcessingPassAsset pass in postProcessingSettings.Passes)
                {
                    if (pass == null)
                    {
                        continue;
                    }

                    mode |= pass.RequiredPrePassMode();
                }
            }

            if (extensionSettings.Extensions != null)
            {
                foreach (ToonRenderingExtensionAsset extension in extensionSettings.Extensions)
                {
                    if (extension == null)
                    {
                        continue;
                    }

                    mode |= extension.RequiredPrePassMode();
                }
            }

            if (settings.IsTiledLightingEnabledAndSupported())
            {
                mode |= PrePassMode.Depth;
            }

            return mode;
        }

        public void Render(
            ScriptableRenderContext context, ref ToonRenderPipelineSharedContext sharedContext,
            Camera camera, ToonAdditionalCameraData additionalCameraData,
            in ToonCameraRendererSettings settings,
            in ToonRampSettings globalRampSettings,
            in ToonShadowSettings shadowSettings,
            in ToonPostProcessingSettings postProcessingSettings,
            in ToonRenderingExtensionSettings extensionSettings)
        {
            _context = context;
            _camera = camera;
            _settings = settings;
            _additionalCameraData = additionalCameraData;

            PrepareForSceneWindow();

            if (!Cull(shadowSettings))
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();
            PrepareBufferName();
            cmd.BeginSample(_cmdName);
            _context.ExecuteCommandBufferAndClear(cmd);

            PrepareMsaa(camera, out int msaaSamples);

            _postProcessing.PreSetup(camera, postProcessingSettings);
            _extensionsCollection.PreSetup(extensionSettings);
            Setup(cmd, globalRampSettings, shadowSettings, extensionSettings, msaaSamples);

            _prePassMode = GetOverridePrePassMode(settings, postProcessingSettings, extensionSettings).Sanitize();
            _opaqueTexture.Setup(ref _context, additionalCameraData, settings);
            _extensionsCollection.Setup(_extensionContext);
            _postProcessing.Setup(_context, postProcessingSettings, _settings,
                additionalCameraData, _renderTarget,
                _renderTarget.ColorFormat, _camera,
                _renderTarget.Width,
                _renderTarget.Height
            );
            _renderTarget.ForceStoreAttachments = _settings.ForceStoreCameraDepth ||
                                                  _opaqueTexture.Enabled ||
                                                  _extensionsCollection.InterruptsGeometryRenderPass() ||
                                                  _postProcessing.InterruptsGeometryRenderPass();

            bool useNativeRenderPasses = _settings.NativeRenderPasses;

#if ENABLE_VR && ENABLE_XR_MODULE
            {
                XRPass xrPass = _additionalCameraData.XrPass;
                if (xrPass.enabled && xrPass.copyDepth)
                {
                    _renderTarget.ForceStoreAttachments = true;

                    // TODO: investigate native render pass support for XR
                    useNativeRenderPasses = false;
                }
            }
#endif // ENABLE_VR && ENABLE_XR_MODULE

            _renderTarget.ConfigureNativeRenderPasses(useNativeRenderPasses);
            _renderTarget.InitState(_additionalCameraData);

            ToonRpUtils.SetupCameraProperties(ref _context, cmd, _additionalCameraData, _camera.projectionMatrix, true);
            SetInverseProjectionMatrix(cmd, _camera.projectionMatrix);
            _context.ExecuteCommandBufferAndClear(cmd);

            _extensionsCollection.RenderEvent(ToonRenderingEvent.BeforePrepass);

            if (_prePassMode != PrePassMode.Off)
            {
                BeginXrRendering(cmd);

                if (_prePassMode.Includes(PrePassMode.Depth))
                {
                    _depthPrePass.Setup(_context, _cullingResults, _camera, _extensionsCollection,
                        _additionalCameraData,
                        settings, _prePassMode,
                        _renderTarget.Width, _renderTarget.Height, _requireStencil
                    );
                    _depthPrePass.Render();
                }

                if (_prePassMode.Includes(PrePassMode.MotionVectors))
                {
                    _motionVectorsPrePass.Setup(_context, _cullingResults, _camera, _extensionsCollection,
                        additionalCameraData,
                        settings,
                        _renderTarget.Width, _renderTarget.Height
                    );
                    _motionVectorsPrePass.Render();
                }

                EndXrRendering(cmd);
            }

            _context.ExecuteCommandBufferAndClear(cmd);
            _extensionsCollection.RenderEvent(ToonRenderingEvent.AfterPrepass);

            SetupProjectionMatricesForMainView(cmd, postProcessingSettings);
            _context.ExecuteCommandBufferAndClear(cmd);

            _tiledLighting.CullLights();

            GeometryRenderPass(cmd, sharedContext);

            if (_postProcessing.AnyFullScreenEffectsEnabled)
            {
                BeginXrRendering(cmd);
                RenderPostProcessing(cmd);
                EndXrRendering(cmd);
            }
            else
            {
                BlitToCameraTarget();
            }

            DrawGizmosPostImageEffects();

            Cleanup(cmd);
            Submit(cmd);
            _additionalCameraData.RestoreProjection();
            CommandBufferPool.Release(cmd);

            if (_renderTarget.CurrentColorBufferId(false) == BuiltinRenderTextureType.CameraTarget)
            {
                sharedContext.NumberOfCamerasUsingBackbuffer++;
            }
        }

        private void PrepareMsaa(Camera camera, out int msaaSamples)
        {
            msaaSamples = (int) _settings.Msaa;
            QualitySettings.antiAliasing = 1;
            msaaSamples = camera.allowMSAA ? msaaSamples : 1;
        }

        partial void PrepareBufferName();

        partial void PrepareForSceneWindow();

        private bool Cull(in ToonShadowSettings toonShadowSettings)
        {
            if (!TryGetCullingParameters(_camera, _additionalCameraData, out ScriptableCullingParameters parameters))
            {
                return false;
            }

            if (_settings.AdditionalLights == AdditionalLightsMode.Off)
            {
                parameters.cullingOptions |= CullingOptions.DisablePerObjectCulling;
            }

            if (toonShadowSettings.Mode == ToonShadowSettings.ShadowMode.ShadowMapping)
            {
                parameters.shadowDistance = Mathf.Min(toonShadowSettings.MaxDistance, _camera.farClipPlane);
            }
            else
            {
                parameters.cullingOptions &= ~CullingOptions.ShadowCasters;
            }

            _cullingResults = _context.Cull(ref parameters);
            return true;
        }

        private static bool TryGetCullingParameters(Camera camera, ToonAdditionalCameraData additionalCameraData,
            out ScriptableCullingParameters cullingParams)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            XRPass xrPass = additionalCameraData.XrPass;
            if (xrPass.enabled)
            {
                cullingParams = xrPass.cullingParams;

                // Sync the FOV on the camera to match the projection from the XR device
                if (!camera.usePhysicalProperties && !XRGraphicsAutomatedTests.enabled)
                {
                    camera.fieldOfView = Mathf.Rad2Deg * Mathf.Atan(1.0f / cullingParams.stereoProjectionMatrix.m11) *
                                         2.0f;
                }

                return true;
            }
#endif // ENABLE_VR && ENABLE_XR_MODULE

            return camera.TryGetCullingParameters(false, out cullingParams);
        }

        private void Setup(CommandBuffer cmd, in ToonRampSettings globalRampSettings,
            in ToonShadowSettings toonShadowSettings, in ToonRenderingExtensionSettings extensionSettings,
            int msaaSamples)
        {
            SetupLighting(cmd, globalRampSettings, toonShadowSettings);
            SetShaderTimeValues(cmd, Time.time);

            float renderScale = _camera.cameraType == CameraType.Game ? _settings.RenderScale : 1.0f;
            int maxRtWidth = int.MaxValue;
            int maxRtHeight = int.MaxValue;
            if (_camera.cameraType == CameraType.Game)
            {
                if (_settings.MaxRenderTextureWidth > 0)
                {
                    maxRtWidth = _settings.MaxRenderTextureWidth;
                }

                if (_settings.MaxRenderTextureHeight > 0)
                {
                    maxRtHeight = _settings.MaxRenderTextureHeight;
                }
            }

            Rect cameraRect = _camera.rect;
            cameraRect.min = Vector2.Max(cameraRect.min, Vector2.zero);
            cameraRect.max = Vector2.Min(cameraRect.max, Vector2.one);
            _camera.rect = cameraRect;
            Rect cameraPixelRect = _camera.pixelRect;

            int rtWidth = _camera.pixelWidth;
            int rtHeight = _camera.pixelHeight;

            GraphicsFormat renderTextureColorFormat = GetRenderTextureColorFormat(_settings);
            if (ToonSceneViewUtils.IsDrawingWireframes(_camera))
            {
                renderTextureColorFormat = GetDefaultGraphicsFormat();
                msaaSamples = 1;
            }

            // Get the maximum supported MSAA level for this RT format
            msaaSamples = SystemInfo.GetRenderTextureSupportedMSAASampleCount(
                new RenderTextureDescriptor(rtWidth, rtHeight, renderTextureColorFormat, 0, 1)
                {
                    msaaSamples = msaaSamples,
                }
            );

            // TODO: investigate whether it is necessary for MSAA: https://github.com/Delt06/toon-rp/issues/188
            bool requireMsaaRenderToTexture = msaaSamples > 1;

#if ENABLE_VR && ENABLE_XR_MODULE
            {
                XRPass xrPass = _additionalCameraData.XrPass;
                if (xrPass.enabled)
                {
                    rtWidth = xrPass.renderTargetDesc.width;
                    rtHeight = xrPass.renderTargetDesc.height;
                    cameraPixelRect = xrPass.GetViewport();
                }
            }
#endif // ENABLE_VR && ENABLE_XR_MODULE

            bool renderToTexture =
                    _settings.ForceRenderToIntermediateBuffer ||
                    requireMsaaRenderToTexture ||
                    renderTextureColorFormat != GetDefaultGraphicsFormat() ||
                    _postProcessing.AnyFullScreenEffectsEnabled ||
                    _opaqueTexture.Enabled ||
                    !Mathf.Approximately(renderScale, 1.0f) ||
                    rtWidth > maxRtWidth ||
                    rtHeight > maxRtHeight
                ;

            if (_camera.cameraType != CameraType.Game)
            {
                renderToTexture = !ToonSceneViewUtils.IsDrawingWireframes(_camera);
            }

            _requireStencil = RequireStencil(extensionSettings);
            GraphicsFormat depthStencilFormat;

            if (renderToTexture)
            {
                depthStencilFormat = ToonFormatUtils.GetDefaultDepthFormat(_requireStencil);

                rtWidth = Mathf.CeilToInt(rtWidth * renderScale);
                rtHeight = Mathf.CeilToInt(rtHeight * renderScale);
                float aspectRatio = (float) rtWidth / rtHeight;

                if (rtWidth > maxRtWidth || rtHeight > maxRtHeight)
                {
                    rtWidth = maxRtWidth;
                    rtHeight = maxRtHeight;
                    bool fixWidth;
                    if (rtWidth == int.MaxValue)
                    {
                        fixWidth = false;
                    }
                    else if (rtHeight == int.MaxValue)
                    {
                        fixWidth = true;
                    }
                    else
                    {
                        fixWidth = aspectRatio > 1;
                    }

                    if (fixWidth)
                    {
                        rtHeight = Mathf.CeilToInt(rtWidth / aspectRatio);
                    }
                    else
                    {
                        rtWidth = Mathf.CeilToInt(rtHeight * aspectRatio);
                    }
                }
            }
            else
            {
                renderTextureColorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
                depthStencilFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);
            }

            _renderTarget.Initialize(_camera, renderToTexture, rtWidth, rtHeight, cameraPixelRect,
                _settings.RenderTextureFilterMode,
                renderTextureColorFormat, depthStencilFormat,
                msaaSamples
            );
            (RenderTargetIdentifier colorBufferId, RenderTargetIdentifier depthBufferId) cameraTarget =
                GetCameraTarget();
            _renderTarget.SetCameraTarget(cameraTarget.colorBufferId, cameraTarget.depthBufferId);

            UpdateRtHandles(rtWidth, rtHeight);

            _cameraData = new ToonCameraData(_camera);

            if (_prePassMode.Includes(PrePassMode.MotionVectors))
            {
                SupportedRenderingFeatures.active.motionVectors = true;
                _additionalCameraData.GetPersistentData<ToonMotionVectorsPersistentData>().Update(_cameraData);
            }

            _extensionContext =
                new ToonRenderingExtensionContext(_extensionsCollection, _context, _camera, _settings, _cullingResults,
                    _renderTarget, _additionalCameraData
                );

            _tiledLighting.Setup(_context, _extensionContext);
        }

        private void SetupProjectionMatricesForMainView(CommandBuffer cmd,
            ToonPostProcessingSettings postProcessingSettings)
        {
            Matrix4x4 jitterMatrix =
                ToonTemporalAAUtils.CalculateJitterMatrix(postProcessingSettings, _camera, _renderTarget);

            ToonMotionVectorsPersistentData motionVectorsPersistentData =
                _additionalCameraData.GetPersistentData<ToonMotionVectorsPersistentData>();
            motionVectorsPersistentData.JitterMatrix = jitterMatrix;
            _additionalCameraData.ViewMatrix = _camera.worldToCameraMatrix;
            _additionalCameraData.BaseProjectionMatrix = _camera.nonJitteredProjectionMatrix;
            _additionalCameraData.JitterMatrix = jitterMatrix;
            _additionalCameraData.JitteredProjectionMatrix = jitterMatrix * _additionalCameraData.BaseProjectionMatrix;
            _additionalCameraData.JitteredGpuProjectionMatrix =
                ToonRpUtils.GetGPUProjectionMatrix(_additionalCameraData.JitteredProjectionMatrix,
                    _renderTarget.RenderToTexture
                );
            ToonRpUtils.SetupCameraProperties(ref _context, cmd,
                _additionalCameraData, _additionalCameraData.JitteredProjectionMatrix, _renderTarget.RenderToTexture
            );

            SetInverseProjectionMatrix(cmd, _additionalCameraData.JitteredProjectionMatrix);
        }

        private static void SetInverseProjectionMatrix(CommandBuffer cmd, Matrix4x4 projectionMatrix)
        {
            var inverseProjectionMatrix =
                Matrix4x4.Inverse(
                    ToonRpUtils.GetGPUProjectionMatrix(projectionMatrix, true)
                );
            cmd.SetGlobalMatrix(ToonRpUtils.ShaderPropertyId.InverseProjectionMatrix,
                inverseProjectionMatrix
            );
        }

        private void UpdateRtHandles(int rtWidth, int rtHeight)
        {
            if (_additionalCameraData.RtWidth != rtWidth || _additionalCameraData.RtHeight != rtHeight)
            {
                _additionalCameraData.RTHandleSystem.ResetReferenceSize(rtWidth, rtHeight);
            }
            else
            {
                _additionalCameraData.RTHandleSystem.SetReferenceSize(rtWidth, rtHeight);
            }

            _additionalCameraData.RtWidth = rtWidth;
            _additionalCameraData.RtHeight = rtHeight;
        }

        private bool RequireStencil(in ToonRenderingExtensionSettings extensionSettings)
        {
            if (_settings.Stencil)
            {
                return true;
            }

            if (extensionSettings.Extensions == null)
            {
                return false;
            }

            foreach (ToonRenderingExtensionAsset extension in extensionSettings.Extensions)
            {
                if (extension == null || !extension.RequiresStencil())
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private void SetupLighting(CommandBuffer cmd, ToonRampSettings globalRampSettings,
            ToonShadowSettings shadowSettings)
        {
            _context.ExecuteCommandBufferAndClear(cmd);

            _globalRamp.Setup(_context, globalRampSettings);

            VisibleLight? mainLight = FindMainLightOrDefault();
            _lighting.Setup(ref _context, _camera, ref _cullingResults, _settings, mainLight);

            {
                _shadows.Setup(_context, _cullingResults, shadowSettings, _camera);
                _shadows.Render(mainLight?.light);
            }
        }

        private VisibleLight? FindMainLightOrDefault()
        {
            foreach (VisibleLight visibleLight in _cullingResults.visibleLights)
            {
                if (visibleLight.lightType == LightType.Directional)
                {
                    return visibleLight;
                }
            }

            return default;
        }

        private void GeometryRenderPass(CommandBuffer cmd, in ToonRenderPipelineSharedContext sharedContext)
        {
            _context.ExecuteCommandBufferAndClear(cmd);

            _extensionsCollection.RenderEvent(ToonRenderingEvent.BeforeGeometryPasses);

            BeginXrRendering(cmd);

            ToonClearValue clearValue = GetRenderTargetsClearValue();
            RenderBufferLoadAction loadAction = sharedContext.NumberOfCamerasUsingBackbuffer == 0
                ? RenderBufferLoadAction.DontCare
                : RenderBufferLoadAction.Load;

            _renderTarget.BeginRenderPass(ref _context, loadAction, clearValue);

            DrawOcclusionMesh(cmd);
            DrawVisibleGeometry(cmd);
            DrawUnsupportedShaders();
            DrawGizmosPreImageEffects();

            _context.ExecuteCommandBufferAndClear(cmd);
            EndXrRendering(cmd);

            _renderTarget.EndRenderPass(ref _context, cmd);
        }

        private void BeginXrRendering(CommandBuffer cmd) =>
            ToonXr.BeginXrRendering(ref _context, cmd, _additionalCameraData.XrPass);

        private void EndXrRendering(CommandBuffer cmd) =>
            ToonXr.EndXrRendering(ref _context, cmd, _additionalCameraData.XrPass);

        private void DrawOcclusionMesh(CommandBuffer cmd) =>
            ToonXr.DrawOcclusionMesh(ref _context, cmd, _additionalCameraData.XrPass);

        private ToonClearValue GetRenderTargetsClearValue()
        {
            CameraClearFlags cameraClearFlags = _camera.clearFlags;
            bool clearDepth = cameraClearFlags <= CameraClearFlags.Depth;
            bool clearColor;
            Color backgroundColor;

#if UNITY_EDITOR
            if (_camera.cameraType == CameraType.Preview)
            {
                clearColor = true;
                backgroundColor = Color.black;
                backgroundColor.r = backgroundColor.g = backgroundColor.b = 0.25f;
            }
            else
#endif // UNITY_EDITOR
            {
                clearColor = cameraClearFlags == CameraClearFlags.Color || _camera.cameraType != CameraType.Game;
                backgroundColor = clearColor ? _camera.backgroundColor.linear : Color.clear;

#if ENABLE_VR && ENABLE_XR_MODULE
                if (_camera.cameraType == CameraType.Game)
                {
                    XRPass xrPass = _additionalCameraData.XrPass;
                    if (xrPass.enabled && !clearColor)
                    {
                        clearColor = true;
                        backgroundColor = Color.black;
                    }
                }
#endif // ENABLE_VR && ENABLE_XR_MODULE
            }

            return new ToonClearValue(clearColor, clearDepth, backgroundColor);
        }

        private void RenderPostProcessing(CommandBuffer cmd)
        {
            RenderTargetIdentifier sourceId = _renderTarget.CurrentColorBufferId();

            _context.ExecuteCommandBufferAndClear(cmd);

            _extensionsCollection.RenderEvent(ToonRenderingEvent.BeforePostProcessing);
            RenderTargetIdentifier destination = _renderTarget.CameraTargetColorId;
            _postProcessing.RenderFullScreenEffects(
                _renderTarget.Width, _renderTarget.Height, _renderTarget.ColorFormat,
                sourceId, destination
            );
            _extensionsCollection.RenderEvent(ToonRenderingEvent.AfterPostProcessing);
        }

        private (RenderTargetIdentifier colorBufferId, RenderTargetIdentifier depthBufferId) GetCameraTarget()
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            XRPass xrPass = _additionalCameraData.XrPass;
            if (xrPass.enabled)
            {
                return (xrPass.renderTarget, xrPass.renderTarget);
            }
#endif // ENABLE_VR && ENABLE_XR_MODULE

            RenderTexture targetTexture = _camera.targetTexture;
            return targetTexture != null
                ? (targetTexture.colorBuffer, targetTexture.depthBuffer)
                : (BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget);
        }


        private void BlitToCameraTarget()
        {
            BeginXrRendering(_finalBlitCmd);

            _renderTarget.FinalBlit(_finalBlitCmd);
            _context.ExecuteCommandBufferAndClear(_finalBlitCmd);

            EndXrRendering(_finalBlitCmd);
        }

        private void Cleanup(CommandBuffer cmd)
        {
            _shadows.Cleanup();

            // Pre-Pass Cleanup
            {
                if (_prePassMode.Includes(PrePassMode.Depth))
                {
                    _depthPrePass.Cleanup();
                }

                if (_prePassMode.Includes(PrePassMode.MotionVectors))
                {
                    _motionVectorsPrePass.Cleanup();
                }
            }

            _opaqueTexture.Cleanup();

            _extensionsCollection.Cleanup();
            _postProcessing.Cleanup();
            _renderTarget.ReleaseTemporaryRTs(cmd);

            _context.ExecuteCommandBufferAndClear(cmd);
        }

        private static void SetShaderTimeValues(CommandBuffer cmd, float time)
        {
            var timeParametersVector = new Vector4(time, Mathf.Sin(time), Mathf.Cos(time), 0.0f);
            cmd.SetGlobalVector(TimeParametersId, timeParametersVector);
        }

        private void Submit(CommandBuffer cmd)
        {
            cmd.EndSample(_cmdName);
            _context.ExecuteCommandBufferAndClear(cmd);
            _context.Submit();
        }

        private void DrawVisibleGeometry(CommandBuffer cmd)
        {
            {
                _extensionsCollection.RenderEvent(ToonRenderingEvent.BeforeOpaque);

                using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.OpaqueGeometry)))
                {
                    _context.ExecuteCommandBufferAndClear(cmd);
                    DrawGeometry(false);
                }

                _context.ExecuteCommandBufferAndClear(cmd);

                _extensionsCollection.RenderEvent(ToonRenderingEvent.AfterOpaque);
            }

            _extensionsCollection.RenderEvent(ToonRenderingEvent.BeforeSkybox);
            DrawSkybox(cmd);
            _extensionsCollection.RenderEvent(ToonRenderingEvent.AfterSkybox);

            _opaqueTexture.Capture();

            {
                _tiledLighting.SetTiledLightingKeyword(cmd, false);
                _context.ExecuteCommandBufferAndClear(cmd);

                _extensionsCollection.RenderEvent(ToonRenderingEvent.BeforeTransparent);

                using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.TransparentGeometry)))
                {
                    _context.ExecuteCommandBufferAndClear(cmd);
                    DrawGeometry(true);
                }

                _context.ExecuteCommandBufferAndClear(cmd);

                _extensionsCollection.RenderEvent(ToonRenderingEvent.AfterTransparent);
            }
        }

        private void DrawGeometry(bool transparent)
        {
            (SortingCriteria sortingCriteria, RenderQueueRange renderQueueRange, int layerMask) = transparent
                ? (SortingCriteria.CommonTransparent, RenderQueueRange.transparent, _settings.TransparentLayerMask)
                : (SortingCriteria.CommonOpaque, RenderQueueRange.opaque, _settings.OpaqueLayerMask);
            var sortingSettings = new SortingSettings(_camera)
            {
                criteria = sortingCriteria,
            };
            DrawGeometry(_settings, ref _context, _cullingResults, sortingSettings, renderQueueRange, transparent,
                layerMask
            );
        }

        public static void DrawGeometry(in ToonCameraRendererSettings settings, ref ScriptableRenderContext context,
            in CullingResults cullingResults, in SortingSettings sortingSettings, RenderQueueRange renderQueueRange,
            bool includesTransparent,
            int layerMask = -1, in RenderStateBlock? renderStateBlock = null,
            IReadOnlyList<ShaderTagId> shaderTagIds = null, bool? perObjectLightDataOverride = null,
            Material overrideMaterial = null)
        {
            PerObjectData perObjectData = PerObjectData.LightProbe;

            bool perObjectLightData =
                perObjectLightDataOverride ?? settings.AdditionalLights != AdditionalLightsMode.Off;
            if (perObjectLightData)
            {
                if (includesTransparent || !settings.IsTiledLightingEnabledAndSupported())
                {
                    perObjectData |= PerObjectData.LightData | PerObjectData.LightIndices;
                }
            }

            shaderTagIds ??= ShaderTagIds;
            var drawingSettings = new DrawingSettings(shaderTagIds[0], sortingSettings)
            {
                enableDynamicBatching = settings.UseDynamicBatching,
                perObjectData = perObjectData,
                overrideMaterial = overrideMaterial,
            };

            for (int i = 0; i < shaderTagIds.Count; i++)
            {
                drawingSettings.SetShaderPassName(i, shaderTagIds[i]);
            }

            var filteringSettings = new FilteringSettings(renderQueueRange, layerMask);

            if (renderStateBlock == null)
            {
                context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
            }
            else
            {
                RenderStateBlock renderStateBlockValue = renderStateBlock.Value;
                context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings,
                    ref renderStateBlockValue
                );
            }
        }

        private void DrawSkybox(CommandBuffer cmd)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            // XRTODO: Remove this code once Skybox pass is moved to SRP land.
            XRPass xrPass = _additionalCameraData.XrPass;
            if (xrPass.enabled)
            {
                // Setup Legacy XR buffer states
                if (xrPass.singlePassEnabled)
                {
                    // Use legacy stereo instancing mode to have legacy XR code path configured
                    cmd.SetSinglePassStereo(SystemInfo.supportsMultiview
                        ? SinglePassStereoMode.Multiview
                        : SinglePassStereoMode.Instancing
                    );
                    _context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    // Calling into built-in skybox pass
                    _context.DrawSkybox(_camera);

                    // Disable Legacy XR path
                    cmd.SetSinglePassStereo(SinglePassStereoMode.None);
                    _context.ExecuteCommandBuffer(cmd);
                    // We do not need to submit here due to special handling of stereo matrices in core.
                    // context.Submit();
                    cmd.Clear();

                    ToonXr.UpdateCameraStereoMatrices(_camera, xrPass);
                }
                else
                {
                    _context.DrawSkybox(_camera);
                }
            }
            else
#endif
            {
                _context.DrawSkybox(_camera);
            }
        }

        partial void DrawGizmosPreImageEffects();
        partial void DrawGizmosPostImageEffects();

        partial void DrawUnsupportedShaders();

        public void InvalidateExtensions()
        {
            _extensionsCollection.Invalidate();
        }
    }
}