using System;
using System.Collections.Generic;
using DELTation.ToonRP.Extensions;
using DELTation.ToonRP.Lighting;
using DELTation.ToonRP.PostProcessing;
using DELTation.ToonRP.Shadows;
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
            new("ToonRPForward"),
            new("SRPDefaultUnlit"),
        };
        private static readonly int PostProcessingSourceId = Shader.PropertyToID("_ToonRP_PostProcessingSource");
        private static readonly int UnityMatrixInvPId = Shader.PropertyToID("unity_MatrixInvP");
        private readonly DepthPrePass _depthPrePass = new();
        private readonly ToonRenderingExtensionsCollection _extensionsCollection = new();
        private readonly CommandBuffer _finalBlitCmd = new() { name = "Final Blit" };
        private readonly ToonGlobalRamp _globalRamp = new();
        private readonly ToonLighting _lighting = new();
        private readonly ToonPostProcessing _postProcessing = new();

        private readonly ToonCameraRenderTarget _renderTarget = new();
        private readonly ToonShadows _shadows = new();
        private readonly ToonTiledLighting _tiledLighting;

        private Camera _camera;

        private string _cmdName = DefaultCmdName;
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;
        private DepthPrePassMode _depthPrePassMode;
        private GraphicsFormat _depthStencilFormat;
        private ToonRenderingExtensionContext _extensionContext;
        private ToonCameraRendererSettings _settings;

        public ToonCameraRenderer() => _tiledLighting = new ToonTiledLighting(_lighting);

        public void Dispose()
        {
            _tiledLighting?.Dispose();
        }

        public static DepthPrePassMode GetOverrideDepthPrePassMode(in ToonCameraRendererSettings settings,
            in ToonPostProcessingSettings postProcessingSettings,
            in ToonRenderingExtensionSettings extensionSettings)
        {
            DepthPrePassMode mode = settings.DepthPrePass;

            if (postProcessingSettings.Passes != null)
            {
                foreach (ToonPostProcessingPassAsset pass in postProcessingSettings.Passes)
                {
                    if (pass == null)
                    {
                        continue;
                    }

                    mode = DepthPrePassModeUtils.CombineDepthPrePassModes(mode, pass.RequiredDepthPrePassMode());
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

                    mode = DepthPrePassModeUtils.CombineDepthPrePassModes(mode, extension.RequiredDepthPrePassMode());
                }
            }

            return mode;
        }

        public void Render(ScriptableRenderContext context, Camera camera, in ToonCameraRendererSettings settings,
            in ToonRampSettings globalRampSettings, in ToonShadowSettings toonShadowSettings,
            in ToonPostProcessingSettings postProcessingSettings,
            in ToonRenderingExtensionSettings extensionSettings)
        {
            _context = context;
            _camera = camera;
            _settings = settings;

            if (!Cull(toonShadowSettings))
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();
            PrepareBufferName();
            cmd.BeginSample(_cmdName);
            _context.ExecuteCommandBufferAndClear(cmd);

            PrepareMsaa(camera, out int msaaSamples);
            PrepareForSceneWindow();

            _depthPrePassMode = GetOverrideDepthPrePassMode(settings, postProcessingSettings, extensionSettings);
            _postProcessing.UpdatePasses(camera, postProcessingSettings);
            Setup(cmd, globalRampSettings, toonShadowSettings, extensionSettings, msaaSamples);
            _extensionsCollection.Update(extensionSettings);
            _extensionsCollection.Setup(_extensionContext);
            _postProcessing.Setup(_context, postProcessingSettings, _settings, _renderTarget.ColorFormat, _camera,
                _renderTarget.Width,
                _renderTarget.Height
            );

            if (_depthPrePassMode != DepthPrePassMode.Off)
            {
                _extensionsCollection.RenderEvent(ToonRenderingEvent.BeforeDepthPrepass);
                _depthPrePass.Setup(_context, _cullingResults, _camera, settings, _depthPrePassMode,
                    _renderTarget.Width, _renderTarget.Height
                );
                _depthPrePass.Render();
                _extensionsCollection.RenderEvent(ToonRenderingEvent.AfterDepthPrepass);
            }

            _tiledLighting.CullLights();

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.PrepareRenderTargets)))
            {
                SetRenderTargets(cmd);
                ClearRenderTargets(cmd);
            }

            DrawVisibleGeometry(cmd);
            DrawUnsupportedShaders();
            DrawGizmosPreImageEffects();

            if (_postProcessing.AnyFullScreenEffectsEnabled)
            {
                RenderPostProcessing(cmd);
            }
            else
            {
                BlitToCameraTarget();
            }

            DrawGizmosPostImageEffects();

            Cleanup(cmd);
            Submit(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void SetRenderTargets(CommandBuffer cmd)
        {
            _renderTarget.SetRenderTarget(cmd);
            _context.ExecuteCommandBufferAndClear(cmd);
        }


        private void PrepareMsaa(Camera camera, out int msaaSamples)
        {
            msaaSamples = (int) _settings.Msaa;
            QualitySettings.antiAliasing = msaaSamples;
            // QualitySettings.antiAliasing returns 0 if MSAA is not supported
            msaaSamples = Mathf.Max(QualitySettings.antiAliasing, 1);
            msaaSamples = camera.allowMSAA ? msaaSamples : 1;
        }

        partial void PrepareBufferName();

        partial void PrepareForSceneWindow();

        private bool Cull(in ToonShadowSettings toonShadowSettings)
        {
            if (!_camera.TryGetCullingParameters(out ScriptableCullingParameters parameters))
            {
                return false;
            }

            if (toonShadowSettings.Mode == ToonShadowSettings.ShadowMode.Vsm)
            {
                parameters.shadowDistance = Mathf.Min(toonShadowSettings.MaxDistance, _camera.farClipPlane);
            }

            _cullingResults = _context.Cull(ref parameters);
            return true;
        }

        private void Setup(CommandBuffer cmd, in ToonRampSettings globalRampSettings,
            in ToonShadowSettings toonShadowSettings, in ToonRenderingExtensionSettings extensionSettings,
            int msaaSamples)
        {
            SetupLighting(cmd, globalRampSettings, toonShadowSettings);

            _context.SetupCameraProperties(_camera);
            Matrix4x4 gpuProjectionMatrix = ToonRpUtils.GetGPUProjectionMatrix(_camera.projectionMatrix);
            cmd.SetGlobalMatrix(UnityMatrixInvPId, Matrix4x4.Inverse(gpuProjectionMatrix));

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

            int rtWidth = _camera.pixelWidth;
            int rtHeight = _camera.pixelHeight;

            static GraphicsFormat GetDefaultGraphicsFormat() =>
                GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default, true);

            static GraphicsFormat GetRenderTextureColorFormat(in ToonCameraRendererSettings settings)
            {
                if (!settings.OverrideRenderTextureFormat)
                {
                    return settings.AllowHdr
                        ? GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.DefaultHDR, false)
                        : GetDefaultGraphicsFormat();
                }

                GraphicsFormat rawGraphicsFormat = settings.RenderTextureFormat;
                FormatUsage formatUsage = FormatUsage.Render;
                formatUsage |= settings.Msaa switch
                {
                    MsaaMode._2x => FormatUsage.MSAA2x,
                    MsaaMode._4x => FormatUsage.MSAA4x,
                    MsaaMode._8x => FormatUsage.MSAA8x,
                    _ => 0,
                };

                return SystemInfo.GetCompatibleFormat(rawGraphicsFormat, formatUsage);
            }

            GraphicsFormat renderTextureColorFormat = GetRenderTextureColorFormat(_settings);
            bool renderToTexture = renderTextureColorFormat != GetDefaultGraphicsFormat() ||
                                   msaaSamples > 1 ||
                                   _postProcessing.AnyFullScreenEffectsEnabled ||
                                   !Mathf.Approximately(renderScale, 1.0f) ||
                                   rtWidth > maxRtWidth ||
                                   rtHeight > maxRtHeight
                ;

            bool requireStencil = RequireStencil(extensionSettings);
            _depthStencilFormat = requireStencil ? GraphicsFormat.D24_UNorm_S8_UInt : GraphicsFormat.D24_UNorm;

            if (renderToTexture)
            {
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

                _renderTarget.InitializeAsSeparateRenderTexture(cmd, _camera, rtWidth, rtHeight,
                    _settings.RenderTextureFilterMode, renderTextureColorFormat, _depthStencilFormat,
                    msaaSamples
                );
            }
            else
            {
                _renderTarget.InitializeAsCameraRenderTarget(_camera, rtWidth, rtHeight, renderTextureColorFormat);
            }

            _context.ExecuteCommandBufferAndClear(cmd);

            _extensionContext =
                new ToonRenderingExtensionContext(_context, _camera, _settings, _cullingResults, _renderTarget);

            _tiledLighting.Setup(_context, _extensionContext);
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

            VisibleLight mainLight = FindMainLightOrDefault();
            _lighting.Setup(ref _context, _camera, ref _cullingResults, _settings, mainLight.light);

            {
                _shadows.Setup(_context, _cullingResults, shadowSettings, _camera);
                _shadows.Render(mainLight.light);
            }
        }

        private VisibleLight FindMainLightOrDefault()
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

        private void ClearRenderTargets(CommandBuffer cmd)
        {
            const string sampleName = "Clear Render Targets";

            cmd.BeginSample(sampleName);

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
            }

            cmd.ClearRenderTarget(clearDepth, clearColor, backgroundColor);

            cmd.EndSample(sampleName);
            _context.ExecuteCommandBufferAndClear(cmd);
        }

        private void RenderPostProcessing(CommandBuffer cmd)
        {
            int sourceId;
            if (_renderTarget.MsaaSamples > 1)
            {
                using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.ResolveCameraColor)))
                {
                    cmd.GetTemporaryRT(
                        PostProcessingSourceId, _camera.pixelWidth, _camera.pixelHeight, 0,
                        _settings.RenderTextureFilterMode, _renderTarget.ColorFormat
                    );
                    cmd.Blit(ToonCameraRenderTarget.CameraColorBufferId, PostProcessingSourceId);
                }

                _context.ExecuteCommandBufferAndClear(cmd);
                sourceId = PostProcessingSourceId;
            }
            else
            {
                sourceId = ToonCameraRenderTarget.CameraColorBufferId;
            }

            _context.ExecuteCommandBufferAndClear(cmd);

            _extensionsCollection.RenderEvent(ToonRenderingEvent.BeforePostProcessing);
            _postProcessing.RenderFullScreenEffects(
                _renderTarget.Width, _renderTarget.Height, _renderTarget.ColorFormat,
                sourceId, BuiltinRenderTextureType.CameraTarget
            );
            _extensionsCollection.RenderEvent(ToonRenderingEvent.AfterPostProcessing);
        }


        private void BlitToCameraTarget()
        {
            _renderTarget.FinalBlit(_finalBlitCmd);
            _context.ExecuteCommandBufferAndClear(_finalBlitCmd);
        }

        private void Cleanup(CommandBuffer cmd)
        {
            _shadows.Cleanup();

            if (_depthPrePassMode != DepthPrePassMode.Off)
            {
                _depthPrePass.Cleanup();
            }

            _extensionsCollection.Cleanup();
            _postProcessing.Cleanup();
            _renderTarget.ReleaseTemporaryRTs(cmd);

            _context.ExecuteCommandBufferAndClear(cmd);
        }

        private void Submit(CommandBuffer cmd)
        {
            cmd.EndSample(_cmdName);
            _context.ExecuteCommandBufferAndClear(cmd);
            _context.Submit();
        }

        private void DrawVisibleGeometry(CommandBuffer cmd)
        {
            _renderTarget.SetScreenParams(cmd);
            _context.ExecuteCommandBufferAndClear(cmd);

            {
                ToonTiledLighting.PrepareForOpaqueGeometry(cmd);

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
            _context.DrawSkybox(_camera);
            _extensionsCollection.RenderEvent(ToonRenderingEvent.AfterSkybox);

            {
                _tiledLighting.PrepareForTransparentGeometry(cmd);

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
            DrawGeometry(_settings, ref _context, _cullingResults, sortingSettings, renderQueueRange, layerMask);
        }

        public static void DrawGeometry(in ToonCameraRendererSettings settings, ref ScriptableRenderContext context,
            in CullingResults cullingResults, in SortingSettings sortingSettings, RenderQueueRange renderQueueRange,
            int layerMask = -1, in RenderStateBlock? renderStateBlock = null,
            IReadOnlyList<ShaderTagId> shaderTagIds = null, bool? perObjectLightDataOverride = null,
            Material overrideMaterial = null)
        {
            PerObjectData perObjectData = PerObjectData.LightProbe;

            bool perObjectLightData =
                perObjectLightDataOverride ?? settings.AdditionalLights != AdditionalLightsMode.Off;
            if (perObjectLightData)
            {
                if (!settings.IsTiledLightingEffectivelyEnabled)
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

        partial void DrawGizmosPreImageEffects();
        partial void DrawGizmosPostImageEffects();

        partial void DrawUnsupportedShaders();
    }
}