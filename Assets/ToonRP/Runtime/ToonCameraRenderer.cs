using UnityEngine;
using UnityEngine.Rendering;

namespace ToonRP.Runtime
{
    public sealed partial class ToonCameraRenderer
    {
        private const string DefaultCmdName = "Render Camera";
        private static readonly ShaderTagId ForwardShaderTagId = new("ToonRPForward");
        private readonly CommandBuffer _cmd = new() { name = DefaultCmdName };
        private readonly ToonGlobalRamp _globalRamp = new();
        private readonly ToonLighting _lighting = new();
        private readonly ToonShadows _shadows = new();

        private Camera _camera;

        private string _cmdName = DefaultCmdName;
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;

        public void Render(ScriptableRenderContext context, Camera camera, in ToonCameraRendererSettings settings,
            in ToonRampSettings globalRampSettings, in ToonShadowSettings toonShadowSettings)
        {
            _context = context;
            _camera = camera;

            PrepareBuffer();
            PrepareForSceneWindow();

            if (!Cull(toonShadowSettings.MaxDistance))
            {
                return;
            }

            Setup(globalRampSettings, toonShadowSettings);

            DrawVisibleGeometry(settings);
            DrawUnsupportedShaders();
            DrawGizmos();

            _shadows.Cleanup();
            Submit();
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

        private void Setup(in ToonRampSettings globalRampSettings, in ToonShadowSettings toonShadowSettings)
        {
            SetupLighting(globalRampSettings, toonShadowSettings);

            _context.SetupCameraProperties(_camera);
            ClearRenderTargets();
            _cmd.BeginSample(_cmdName);

            ExecuteBuffer();
        }

        private void SetupLighting(ToonRampSettings globalRampSettings, ToonShadowSettings toonShadowSettings)
        {
            _cmd.BeginSample(_cmdName);
            ExecuteBuffer();

            _globalRamp.Setup(_context, globalRampSettings);

            Light sun = RenderSettings.sun;
            _lighting.Setup(_context, sun);

            {
                _shadows.Setup(_context, _cullingResults, toonShadowSettings);

                for (int visibleLightIndex = 0;
                     visibleLightIndex < _cullingResults.visibleLights.Length;
                     visibleLightIndex++)
                {
                    VisibleLight visibleLight = _cullingResults.visibleLights[visibleLightIndex];
                    if (visibleLight.light != sun)
                    {
                        continue;
                    }

                    _shadows.ReserveDirectionalShadows(visibleLight.light, visibleLightIndex);
                }

                _shadows.Render();
            }

            _cmd.EndSample(_cmdName);
        }

        private void ClearRenderTargets()
        {
            const string sampleName = "Clear Render Targets";

            _cmd.BeginSample(sampleName);

            CameraClearFlags cameraClearFlags = _camera.clearFlags;
            bool clearDepth = cameraClearFlags <= CameraClearFlags.Depth;
            bool clearColor = cameraClearFlags == CameraClearFlags.Color;
            Color backgroundColor = clearColor ? _camera.backgroundColor.linear : Color.clear;
            _cmd.ClearRenderTarget(clearDepth, clearColor, backgroundColor);

            _cmd.EndSample(sampleName);
        }

        private void Submit()
        {
            _cmd.EndSample(_cmdName);
            ExecuteBuffer();
            _context.Submit();
        }

        private void ExecuteBuffer()
        {
            _context.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }

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