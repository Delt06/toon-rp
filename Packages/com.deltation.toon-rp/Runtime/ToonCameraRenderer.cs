using DELTation.ToonRP.PostProcessing;
using DELTation.ToonRP.Shadows;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using static DELTation.ToonRP.ToonCameraRendererSettings;

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

        private static readonly int CameraColorBufferId = Shader.PropertyToID("_ToonRP_CameraColorBuffer");
        private static readonly int PostProcessingSourceId = Shader.PropertyToID("_ToonRP_PostProcessingSource");
        private static readonly int CameraDepthBufferId = Shader.PropertyToID("_ToonRP_CameraDepthBuffer");
        private static readonly int ScreenParamsId = Shader.PropertyToID("_ToonRP_ScreenParams");
        private static readonly int UnityMatrixInvPId = Shader.PropertyToID("unity_MatrixInvP");
        private readonly DepthPrePass _depthPrePass = new();
        private readonly CommandBuffer _finalBlitCmd = new() { name = "Final Blit" };
        private readonly ToonGlobalRamp _globalRamp = new();
        private readonly ToonInvertedHullOutline _invertedHullOutline = new();
        private readonly ToonLighting _lighting = new();
        private readonly ToonPostProcessing _postProcessing = new();
        private readonly ToonShadows _shadows = new();
        private readonly ToonSsao _ssao = new();

        private Camera _camera;

        private string _cmdName = DefaultCmdName;
        private RenderTextureFormat _colorFormat;
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;
        private GraphicsFormat _depthStencilFormat;
        private bool _drawInvertedHullOutlines;
        private int _msaaSamples;
        private bool _renderToTexture;
        private int _rtHeight;
        private int _rtWidth;
        private ToonCameraRendererSettings _settings;

        public void Render(ScriptableRenderContext context, Camera camera, in ToonCameraRendererSettings settings,
            in ToonRampSettings globalRampSettings, in ToonShadowSettings toonShadowSettings,
            in ToonPostProcessingSettings postProcessingSettings, in ToonSsaoSettings ssaoSettings)
        {
            _context = context;
            _camera = camera;
            _settings = settings;

            CommandBuffer cmd = CommandBufferPool.Get();
            PrepareBufferName();
            cmd.BeginSample(_cmdName);

            PrepareMsaa(camera);
            PrepareForSceneWindow();

            if (!Cull(toonShadowSettings))
            {
                return;
            }

            _postProcessing.UpdatePasses(camera, postProcessingSettings);
            Setup(cmd, globalRampSettings, toonShadowSettings, postProcessingSettings);
            _postProcessing.Setup(_context, postProcessingSettings, _settings, _colorFormat, _camera, _rtWidth,
                _rtHeight
            );
            _drawInvertedHullOutlines = postProcessingSettings.Enabled &&
                                        postProcessingSettings.Outline.Mode ==
                                        ToonOutlineSettings.OutlineMode.InvertedHull;
            if (_drawInvertedHullOutlines)
            {
                _invertedHullOutline.Setup(_context, _cullingResults, _camera, settings,
                    postProcessingSettings.Outline.InvertedHull
                );
            }

            if (settings.DepthPrePass != DepthPrePassMode.Off)
            {
                _depthPrePass.Setup(_context, _cullingResults, _camera, settings, _rtWidth, _rtHeight);
                _depthPrePass.Render();
            }

            if (ssaoSettings.Enabled)
            {
                _ssao.Setup(_context, ssaoSettings, _rtWidth, _rtHeight);
                _ssao.Render();
            }
            else
            {
                _ssao.SetupDisabled(_context);
            }

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.PrepareRenderTargets)))
            {
                SetRenderTargets(cmd);
                ClearRenderTargets(cmd);
            }

            DrawVisibleGeometry(cmd);
            DrawUnsupportedShaders();
            DrawGizmos();

            if (_postProcessing.AnyFullScreenEffectsEnabled)
            {
                RenderPostProcessing(cmd);
            }
            else
            {
                BlitToCameraTarget();
            }

            Cleanup(cmd);
            Submit(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void SetRenderTargets(CommandBuffer cmd)
        {
            if (_renderToTexture)
            {
                cmd.SetRenderTarget(
                    CameraColorBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    CameraDepthBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                );
            }
            else
            {
                cmd.SetRenderTarget(
                    BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store
                );
            }

            ExecuteBuffer(cmd);
        }


        private void PrepareMsaa(Camera camera)
        {
            _msaaSamples = (int) _settings.Msaa;
            QualitySettings.antiAliasing = _msaaSamples;
            // QualitySettings.antiAliasing returns 0 if MSAA is not supported
            _msaaSamples = Mathf.Max(QualitySettings.antiAliasing, 1);
            _msaaSamples = camera.allowMSAA ? _msaaSamples : 1;
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
            in ToonShadowSettings toonShadowSettings, in ToonPostProcessingSettings postProcessingSettings)
        {
            SetupLighting(cmd, globalRampSettings, toonShadowSettings);

            _context.SetupCameraProperties(_camera);
            Matrix4x4 gpuProjectionMatrix =
                GL.GetGPUProjectionMatrix(_camera.projectionMatrix, SystemInfo.graphicsUVStartsAtTop);
            cmd.SetGlobalMatrix(UnityMatrixInvPId, Matrix4x4.Inverse(gpuProjectionMatrix));

            float renderScale = _camera.cameraType == CameraType.Game ? _settings.RenderScale : 1.0f;

            _renderToTexture = _settings.AllowHdr || _msaaSamples > 1 ||
                               _postProcessing.AnyFullScreenEffectsEnabled ||
                               !Mathf.Approximately(renderScale, 1.0f)
                ;
            _colorFormat = _settings.AllowHdr ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            bool requireStencil = _settings.Stencil || InvertedHullOutlinesRequireStencil(postProcessingSettings);
            _depthStencilFormat = requireStencil ? GraphicsFormat.D24_UNorm_S8_UInt : GraphicsFormat.D24_UNorm;

            _rtWidth = _camera.pixelWidth;
            _rtHeight = _camera.pixelHeight;

            if (_renderToTexture)
            {
                _rtWidth = Mathf.CeilToInt(_rtWidth * renderScale);
                _rtHeight = Mathf.CeilToInt(_rtHeight * renderScale);
                cmd.GetTemporaryRT(
                    CameraColorBufferId, _rtWidth, _rtHeight, 0,
                    _settings.RenderTextureFilterMode, _colorFormat,
                    RenderTextureReadWrite.Default, _msaaSamples
                );

                var depthDesc = new RenderTextureDescriptor(_rtWidth, _rtHeight,
                    GraphicsFormat.None, _depthStencilFormat,
                    0
                )
                {
                    msaaSamples = _msaaSamples,
                };
                cmd.GetTemporaryRT(CameraDepthBufferId, depthDesc, FilterMode.Point);
            }

            ExecuteBuffer(cmd);
        }

        private static bool InvertedHullOutlinesRequireStencil(in ToonPostProcessingSettings postProcessingSettings)
        {
            if (postProcessingSettings.Outline.Mode != ToonOutlineSettings.OutlineMode.InvertedHull)
            {
                return false;
            }

            foreach (ToonInvertedHullOutlineSettings.Pass pass in postProcessingSettings.Outline.InvertedHull.Passes)
            {
                if (pass.StencilLayer != StencilLayer.None)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetupLighting(CommandBuffer cmd, ToonRampSettings globalRampSettings,
            ToonShadowSettings shadowSettings)
        {
            ExecuteBuffer(cmd);

            _globalRamp.Setup(_context, globalRampSettings);


            VisibleLight visibleLight =
                _cullingResults.visibleLights.Length > 0 ? _cullingResults.visibleLights[0] : default;
            _lighting.Setup(_context, visibleLight.light);

            {
                _shadows.Setup(_context, _cullingResults, shadowSettings, _camera);
                _shadows.Render(visibleLight.light);
            }
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
            if (_msaaSamples > 1)
            {
                using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.ResolveCameraColor)))
                {
                    cmd.GetTemporaryRT(
                        PostProcessingSourceId, _camera.pixelWidth, _camera.pixelHeight, 0,
                        _settings.RenderTextureFilterMode, _colorFormat,
                        RenderTextureReadWrite.Default
                    );
                    cmd.Blit(CameraColorBufferId, PostProcessingSourceId);
                }

                ExecuteBuffer(cmd);
                sourceId = PostProcessingSourceId;
            }
            else
            {
                sourceId = CameraColorBufferId;
            }

            ExecuteBuffer(cmd);
            _postProcessing.RenderFullScreenEffects(
                _rtWidth, _rtHeight, _colorFormat,
                sourceId, BuiltinRenderTextureType.CameraTarget
            );
        }


        private void BlitToCameraTarget()
        {
            if (_renderToTexture)
            {
                _finalBlitCmd.Blit(CameraColorBufferId, BuiltinRenderTextureType.CameraTarget);
                ExecuteBuffer(_finalBlitCmd);
            }
        }

        private void Cleanup(CommandBuffer cmd)
        {
            _shadows.Cleanup();

            if (_settings.DepthPrePass != DepthPrePassMode.Off)
            {
                _depthPrePass.Cleanup();
            }

            _ssao.Cleanup();
            _postProcessing.Cleanup();

            if (_renderToTexture)
            {
                cmd.ReleaseTemporaryRT(CameraColorBufferId);
                cmd.ReleaseTemporaryRT(CameraDepthBufferId);
            }

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
            cmd.SetGlobalVector(ScreenParamsId, new Vector4(
                    1.0f / _rtWidth,
                    1.0f / _rtHeight,
                    _rtWidth,
                    _rtHeight
                )
            );
            ExecuteBuffer(cmd);

            {
                using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.OpaqueGeometry)))
                {
                    ExecuteBuffer(cmd);
                    DrawGeometry(RenderQueueRange.opaque);
                }

                ExecuteBuffer(cmd);
            }


            if (_drawInvertedHullOutlines)
            {
                _invertedHullOutline.Render();
            }

            _context.DrawSkybox(_camera);

            {
                using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.TransparentGeometry)))
                {
                    ExecuteBuffer(cmd);
                    DrawGeometry(RenderQueueRange.transparent);
                }

                ExecuteBuffer(cmd);
            }
        }

        private void DrawGeometry(RenderQueueRange renderQueueRange)
        {
            var sortingSettings = new SortingSettings(_camera)
            {
                criteria = SortingCriteria.CommonOpaque,
            };
            var drawingSettings = new DrawingSettings(ShaderTagIds[0], sortingSettings)
            {
                enableDynamicBatching = _settings.UseDynamicBatching,
                perObjectData = PerObjectData.LightProbe,
            };

            for (int i = 0; i < ShaderTagIds.Length; i++)
            {
                drawingSettings.SetShaderPassName(i, ShaderTagIds[i]);
            }

            var filteringSettings = new FilteringSettings(renderQueueRange);

            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }

        partial void DrawGizmos();

        partial void DrawUnsupportedShaders();
    }
}