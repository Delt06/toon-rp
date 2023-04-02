using ToonRP.Runtime.PostProcessing;
using ToonRP.Runtime.Shadows;
using UnityEngine;
using UnityEngine.Rendering;
using static ToonRP.Runtime.ToonCameraRendererSettings;

namespace ToonRP.Runtime
{
    public sealed partial class ToonCameraRenderer
    {
        private const string DefaultCmdName = "Render Camera";
        public static readonly ShaderTagId ForwardShaderTagId = new("ToonRPForward");

        private static readonly int CameraColorBufferId = Shader.PropertyToID("_ToonRP_CameraColorBuffer");
        private static readonly int PostProcessingSourceId = Shader.PropertyToID("_ToonRP_PostProcessingSource");
        private static readonly int CameraDepthBufferId = Shader.PropertyToID("_ToonRP_CameraDepthBuffer");
        private readonly CommandBuffer _cmd = new() { name = DefaultCmdName };
        private readonly DepthPrePass _depthPrePass = new();
        private readonly CommandBuffer _finalBlitCmd = new() { name = "Final Blit" };
        private readonly ToonGlobalRamp _globalRamp = new();
        private readonly ToonInvertedHullOutline _invertedHullOutline = new();
        private readonly ToonLighting _lighting = new();
        private readonly ToonPostProcessing _postProcessing = new();
        private readonly CommandBuffer _prepareRtCmd = new() { name = "Prepare Render Targets" };
        private readonly ToonShadows _shadows = new();
        private readonly ToonSsao _ssao = new();

        private Camera _camera;

        private string _cmdName = DefaultCmdName;
        private RenderTextureFormat _colorFormat;
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;
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

            PrepareMsaa(camera);
            PrepareBuffer();
            PrepareForSceneWindow();

            if (!Cull(toonShadowSettings))
            {
                return;
            }

            Setup(globalRampSettings, toonShadowSettings, postProcessingSettings);
            _postProcessing.Setup(_context, postProcessingSettings, _colorFormat, _camera, _rtWidth, _rtHeight);
            bool drawInvertedHullOutlines =
                postProcessingSettings.Enabled &&
                postProcessingSettings.Outline.Mode == ToonOutlineSettings.OutlineMode.InvertedHull;
            if (drawInvertedHullOutlines)
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

            SetRenderTargets();
            ClearRenderTargets();

            if (drawInvertedHullOutlines)
            {
                _invertedHullOutline.Render();
            }


            DrawVisibleGeometry();
            DrawUnsupportedShaders();
            DrawGizmos();

            if (_postProcessing.IsActive)
            {
                RenderPostProcessing();
            }
            else
            {
                BlitToCameraTarget();
            }

            Cleanup();
            Submit();
        }

        private void SetRenderTargets()
        {
            if (_renderToTexture)
            {
                _cmd.SetRenderTarget(
                    CameraColorBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    CameraDepthBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                );
            }
            else
            {
                _cmd.SetRenderTarget(
                    BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store
                );
            }

            ExecuteBuffer(_cmd);
        }


        private void PrepareMsaa(Camera camera)
        {
            _msaaSamples = (int) _settings.Msaa;
            QualitySettings.antiAliasing = _msaaSamples;
            // QualitySettings.antiAliasing returns 0 if MSAA is not supported
            _msaaSamples = Mathf.Max(QualitySettings.antiAliasing, 1);
            _msaaSamples = camera.allowMSAA ? _msaaSamples : 1;
        }

        partial void PrepareBuffer();

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

        private void Setup(in ToonRampSettings globalRampSettings,
            in ToonShadowSettings toonShadowSettings, in ToonPostProcessingSettings postProcessingSettings)
        {
            SetupLighting(globalRampSettings, toonShadowSettings);

            _context.SetupCameraProperties(_camera);
            Matrix4x4 gpuProjectionMatrix =
                GL.GetGPUProjectionMatrix(_camera.projectionMatrix, SystemInfo.graphicsUVStartsAtTop);
            _cmd.SetGlobalMatrix("unity_MatrixInvP", Matrix4x4.Inverse(gpuProjectionMatrix));

            _renderToTexture = _settings.AllowHdr || _msaaSamples > 1 || postProcessingSettings.Enabled;
            _colorFormat = _settings.AllowHdr ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

            if (_renderToTexture)
            {
                _rtWidth = _camera.pixelWidth;
                _rtHeight = _camera.pixelHeight;
                _cmd.GetTemporaryRT(
                    CameraColorBufferId, _rtWidth, _rtHeight, 0,
                    FilterMode.Bilinear, _colorFormat,
                    RenderTextureReadWrite.Default, _msaaSamples
                );
                _cmd.GetTemporaryRT(
                    CameraDepthBufferId, _rtWidth, _rtHeight, 24,
                    FilterMode.Point, RenderTextureFormat.Depth,
                    RenderTextureReadWrite.Linear, _msaaSamples
                );
            }

            _cmd.BeginSample(_cmdName);

            ExecuteBuffer();
        }

        private void SetupLighting(ToonRampSettings globalRampSettings, ToonShadowSettings shadowSettings)
        {
            _cmd.BeginSample(_cmdName);
            ExecuteBuffer();

            _globalRamp.Setup(_context, globalRampSettings);


            VisibleLight visibleLight =
                _cullingResults.visibleLights.Length > 0 ? _cullingResults.visibleLights[0] : default;
            _lighting.Setup(_context, visibleLight.light);

            {
                _shadows.Setup(_context, _cullingResults, shadowSettings, _camera);
                _shadows.Render(visibleLight.light);
            }

            _cmd.EndSample(_cmdName);
        }

        private void ClearRenderTargets()
        {
            const string sampleName = "Clear Render Targets";

            _prepareRtCmd.BeginSample(sampleName);

            CameraClearFlags cameraClearFlags = _camera.clearFlags;
            bool clearDepth = cameraClearFlags <= CameraClearFlags.Depth;
            bool clearColor = cameraClearFlags == CameraClearFlags.Color;
            Color backgroundColor = clearColor ? _camera.backgroundColor.linear : Color.clear;
            _prepareRtCmd.ClearRenderTarget(clearDepth, clearColor, backgroundColor);

            _prepareRtCmd.EndSample(sampleName);
            ExecuteBuffer(_prepareRtCmd);
        }

        private void RenderPostProcessing()
        {
            int sourceId;
            if (_msaaSamples > 1)
            {
                const string sampleName = "Resolve Camera Color";
                _cmd.BeginSample(sampleName);
                _cmd.GetTemporaryRT(
                    PostProcessingSourceId, _camera.pixelWidth, _camera.pixelHeight, 0,
                    FilterMode.Point, _colorFormat,
                    RenderTextureReadWrite.Default
                );
                _cmd.Blit(CameraColorBufferId, PostProcessingSourceId);
                _cmd.EndSample(sampleName);
                ExecuteBuffer();
                sourceId = PostProcessingSourceId;
            }
            else
            {
                sourceId = CameraColorBufferId;
            }

            ExecuteBuffer();
            _postProcessing.Render(
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

        private void Cleanup()
        {
            _shadows.Cleanup();

            if (_settings.DepthPrePass != DepthPrePassMode.Off)
            {
                _depthPrePass.Cleanup();
            }

            _ssao.Cleanup();

            if (_renderToTexture)
            {
                _cmd.ReleaseTemporaryRT(CameraColorBufferId);
                _cmd.ReleaseTemporaryRT(CameraDepthBufferId);
            }

            ExecuteBuffer();
        }

        private void Submit()
        {
            _cmd.EndSample(_cmdName);
            ExecuteBuffer();
            _context.Submit();
        }

        private void ExecuteBuffer(CommandBuffer cmd)
        {
            _context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        private void ExecuteBuffer() => ExecuteBuffer(_cmd);

        private void DrawVisibleGeometry()
        {
            DrawOpaqueGeometry();
            _context.DrawSkybox(_camera);
        }

        private void DrawOpaqueGeometry()
        {
            _cmd.SetGlobalVector("_ToonRP_ScreenParams", new Vector4(
                    1.0f / _rtWidth,
                    1.0f / _rtHeight,
                    _rtWidth,
                    _rtHeight
                )
            );
            ExecuteBuffer();

            var sortingSettings = new SortingSettings(_camera)
            {
                criteria = SortingCriteria.CommonOpaque,
            };
            var drawingSettings = new DrawingSettings(ForwardShaderTagId, sortingSettings)
            {
                enableDynamicBatching = _settings.UseDynamicBatching,
                perObjectData = PerObjectData.LightProbe,
            };
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }

        partial void DrawGizmos();

        partial void DrawUnsupportedShaders();
    }
}