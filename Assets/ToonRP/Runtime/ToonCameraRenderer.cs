using ToonRP.Runtime.PostProcessing;
using UnityEngine;
using UnityEngine.Rendering;

namespace ToonRP.Runtime
{
    public sealed partial class ToonCameraRenderer
    {
        private const string DefaultCmdName = "Render Camera";
        private static readonly ShaderTagId ForwardShaderTagId = new("ToonRPForward");

        private static readonly int CameraColorBufferId = Shader.PropertyToID("_ToonRP_CameraColorBuffer");
        private static readonly int PostProcessingSourceId = Shader.PropertyToID("_ToonRP_PostProcessingSource");
        private static readonly int CameraDepthBufferId = Shader.PropertyToID("_ToonRP_CameraDepthBuffer");
        private readonly CommandBuffer _cmd = new() { name = DefaultCmdName };
        private readonly CommandBuffer _finalBlitCmd = new() { name = "Final Blit" };
        private readonly ToonGlobalRamp _globalRamp = new();
        private readonly ToonLighting _lighting = new();
        private readonly ToonPostProcessing _postProcessing = new();
        private readonly CommandBuffer _prepareRtCmd = new() { name = "Prepare Render Targets" };
        private readonly ToonShadows _shadows = new();

        private Camera _camera;

        private string _cmdName = DefaultCmdName;
        private RenderTextureFormat _colorFormat;
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;
        private int _msaaSamples;
        private bool _renderToTexture;

        public void Render(ScriptableRenderContext context, Camera camera, in ToonCameraRendererSettings settings,
            in ToonRampSettings globalRampSettings, in ToonShadowSettings toonShadowSettings,
            in ToonPostProcessingSettings postProcessingSettings)
        {
            _context = context;
            _camera = camera;

            PrepareMsaa(camera, settings);
            PrepareBuffer();
            PrepareForSceneWindow();

            if (!Cull(toonShadowSettings.MaxDistance))
            {
                return;
            }

            Setup(settings, globalRampSettings, toonShadowSettings, postProcessingSettings);
            _postProcessing.Setup(_context, postProcessingSettings, _colorFormat, _camera);

            SetRenderTargets();
            ClearRenderTargets();

            DrawVisibleGeometry(settings);
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


        private void PrepareMsaa(Camera camera, ToonCameraRendererSettings settings)
        {
            _msaaSamples = (int) settings.Msaa;
            QualitySettings.antiAliasing = _msaaSamples;
            // QualitySettings.antiAliasing returns 0 if MSAA is not supported
            _msaaSamples = Mathf.Max(QualitySettings.antiAliasing, 1);
            _msaaSamples = camera.allowMSAA ? _msaaSamples : 1;
        }

        partial void PrepareBuffer();

        partial void PrepareForSceneWindow();

        private bool Cull(float maxShadowDistance)
        {
            if (!_camera.TryGetCullingParameters(out ScriptableCullingParameters parameters))
            {
                return false;
            }

            parameters.shadowDistance = Mathf.Min(maxShadowDistance, _camera.farClipPlane);
            _cullingResults = _context.Cull(ref parameters);
            return true;
        }

        private void Setup(in ToonCameraRendererSettings settings, in ToonRampSettings globalRampSettings,
            in ToonShadowSettings toonShadowSettings, in ToonPostProcessingSettings postProcessingSettings)
        {
            SetupLighting(globalRampSettings, toonShadowSettings);

            _context.SetupCameraProperties(_camera);
            _renderToTexture = settings.AllowHdr || _msaaSamples > 1 || postProcessingSettings.Enabled;
            _colorFormat = settings.AllowHdr ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

            if (_renderToTexture)
            {
                _cmd.GetTemporaryRT(
                    CameraColorBufferId, _camera.pixelWidth, _camera.pixelHeight, 0,
                    FilterMode.Bilinear, _colorFormat,
                    RenderTextureReadWrite.Default, _msaaSamples
                );
                _cmd.GetTemporaryRT(
                    CameraDepthBufferId, _camera.pixelWidth, _camera.pixelHeight, 24,
                    FilterMode.Point, RenderTextureFormat.Depth,
                    RenderTextureReadWrite.Linear, _msaaSamples
                );
            }

            _cmd.BeginSample(_cmdName);

            ExecuteBuffer();
        }

        private void SetupLighting(ToonRampSettings globalRampSettings, ToonShadowSettings toonShadowSettings)
        {
            _cmd.BeginSample(_cmdName);
            ExecuteBuffer();

            _globalRamp.Setup(_context, globalRampSettings);


            VisibleLight visibleLight =
                _cullingResults.visibleLights.Length > 0 ? _cullingResults.visibleLights[0] : default;
            _lighting.Setup(_context, visibleLight.light);

            {
                _shadows.Setup(_context, _cullingResults, toonShadowSettings);
                if (visibleLight.light != null)
                {
                    _shadows.ReserveDirectionalShadows(visibleLight.light, 0);
                }

                _shadows.Render();
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

        private void DrawVisibleGeometry(in ToonCameraRendererSettings settings)
        {
            DrawOpaqueGeometry(settings);
            _context.DrawSkybox(_camera);
        }

        private void DrawOpaqueGeometry(in ToonCameraRendererSettings settings)
        {
            var sortingSettings = new SortingSettings(_camera)
            {
                criteria = SortingCriteria.CommonOpaque,
            };
            var drawingSettings = new DrawingSettings(ForwardShaderTagId, sortingSettings)
            {
                enableDynamicBatching = settings.UseDynamicBatching,
            };
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }

        partial void DrawGizmos();

        partial void DrawUnsupportedShaders();
    }
}