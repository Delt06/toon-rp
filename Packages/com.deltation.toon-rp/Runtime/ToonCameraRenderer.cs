using DELTation.ToonRP.Extensions;
using DELTation.ToonRP.PostProcessing;
using DELTation.ToonRP.Shadows;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public sealed partial class ToonCameraRenderer
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

        private Camera _camera;

        private string _cmdName = DefaultCmdName;
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;
        private DepthPrePassMode _depthPrePassMode;
        private GraphicsFormat _depthStencilFormat;
        private ToonRenderingExtensionContext _extensionContext;
        private ToonCameraRendererSettings _settings;

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

            CommandBuffer cmd = CommandBufferPool.Get();
            PrepareBufferName();
            cmd.BeginSample(_cmdName);

            PrepareMsaa(camera, out int msaaSamples);
            PrepareForSceneWindow();

            if (!Cull(toonShadowSettings))
            {
                return;
            }

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
            ExecuteBuffer(cmd);
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
            Matrix4x4 gpuProjectionMatrix =
                GL.GetGPUProjectionMatrix(_camera.projectionMatrix, SystemInfo.graphicsUVStartsAtTop);
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

            bool renderToTexture = _settings.AllowHdr || msaaSamples > 1 ||
                                   _postProcessing.AnyFullScreenEffectsEnabled ||
                                   !Mathf.Approximately(renderScale, 1.0f) ||
                                   rtWidth > maxRtWidth ||
                                   rtHeight > maxRtHeight
                ;
            RenderTextureFormat colorFormat =
                _settings.AllowHdr ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
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

                _renderTarget.InitializeAsSeparateRenderTexture(cmd, rtWidth, rtHeight,
                    _settings.RenderTextureFilterMode, colorFormat, _depthStencilFormat,
                    msaaSamples
                );
            }
            else
            {
                _renderTarget.InitializeAsCameraRenderTarget(rtWidth, rtHeight, colorFormat);
            }

            ExecuteBuffer(cmd);

            _extensionContext =
                new ToonRenderingExtensionContext(_context, _camera, _settings, _cullingResults, _renderTarget);
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
            ExecuteBuffer(cmd);

            _globalRamp.Setup(_context, globalRampSettings);

            VisibleLight mainLight = FindMainLightOrDefault();
            _lighting.Setup(ref _context, ref _cullingResults, _settings, mainLight.light);

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
                clearColor = cameraClearFlags == CameraClearFlags.Color;
                backgroundColor = clearColor ? _camera.backgroundColor.linear : Color.clear;
            }

            cmd.ClearRenderTarget(clearDepth, clearColor, backgroundColor);

            cmd.EndSample(sampleName);
            ExecuteBuffer(cmd);
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
                        _settings.RenderTextureFilterMode, _renderTarget.ColorFormat,
                        RenderTextureReadWrite.Default
                    );
                    cmd.Blit(ToonCameraRenderTarget.CameraColorBufferId, PostProcessingSourceId);
                }

                ExecuteBuffer(cmd);
                sourceId = PostProcessingSourceId;
            }
            else
            {
                sourceId = ToonCameraRenderTarget.CameraColorBufferId;
            }

            ExecuteBuffer(cmd);

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
            ExecuteBuffer(_finalBlitCmd);
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

            ExecuteBuffer(cmd);
        }

        private void Submit(CommandBuffer cmd)
        {
            cmd.EndSample(_cmdName);
            ExecuteBuffer(cmd);
            _context.Submit();
        }

        private void ExecuteBuffer(CommandBuffer cmd)
        {
            _context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        private void DrawVisibleGeometry(CommandBuffer cmd)
        {
            _renderTarget.SetScreenParams(cmd);
            ExecuteBuffer(cmd);

            {
                _extensionsCollection.RenderEvent(ToonRenderingEvent.BeforeOpaque);

                using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.OpaqueGeometry)))
                {
                    ExecuteBuffer(cmd);
                    DrawGeometry(false);
                }

                ExecuteBuffer(cmd);

                _extensionsCollection.RenderEvent(ToonRenderingEvent.AfterOpaque);
            }

            _extensionsCollection.RenderEvent(ToonRenderingEvent.BeforeSkybox);
            _context.DrawSkybox(_camera);
            _extensionsCollection.RenderEvent(ToonRenderingEvent.AfterSkybox);

            {
                _extensionsCollection.RenderEvent(ToonRenderingEvent.BeforeTransparent);

                using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.TransparentGeometry)))
                {
                    ExecuteBuffer(cmd);
                    DrawGeometry(true);
                }

                ExecuteBuffer(cmd);

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
            DrawGeometry(_settings, ref _context, _cullingResults, sortingSettings, renderQueueRange,
                _settings.AdditionalLights, layerMask
            );
        }

        public static void DrawGeometry(in ToonCameraRendererSettings settings, ref ScriptableRenderContext context,
            in CullingResults cullingResults, in SortingSettings sortingSettings, RenderQueueRange renderQueueRange,
            bool perObjectLightData,
            int layerMask = -1, RenderStateBlock renderStateBlock = default)
        {
            PerObjectData perObjectData = PerObjectData.LightProbe;
            if (perObjectLightData)
            {
                perObjectData |= PerObjectData.LightData | PerObjectData.LightIndices;
            }

            var drawingSettings = new DrawingSettings(ShaderTagIds[0], sortingSettings)
            {
                enableDynamicBatching = settings.UseDynamicBatching,
                perObjectData = perObjectData,
            };

            for (int i = 0; i < ShaderTagIds.Length; i++)
            {
                drawingSettings.SetShaderPassName(i, ShaderTagIds[i]);
            }

            var filteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
        }

        partial void DrawGizmosPreImageEffects();
        partial void DrawGizmosPostImageEffects();

        partial void DrawUnsupportedShaders();
    }
}