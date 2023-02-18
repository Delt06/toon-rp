using UnityEngine;
using UnityEngine.Rendering;

namespace ToonRP.Runtime
{
    public sealed partial class ToonCameraRenderer
    {
        private const string DefaultBufferName = "Render Camera";
        private static readonly ShaderTagId ForwardShaderTagId = new("ToonRPForward");
        private readonly CommandBuffer _buffer = new() { name = DefaultBufferName };
        private readonly ToonGlobalRamp _globalRamp = new();
        private readonly ToonLighting _lighting = new();

        private string _bufferName = DefaultBufferName;

        private Camera _camera;
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;

        public void Render(ScriptableRenderContext context, Camera camera, in ToonCameraRendererSettings settings,
            in ToonRampSettings globalRampSettings)
        {
            _context = context;
            _camera = camera;

            PrepareBuffer();
            PrepareForSceneWindow();

            if (!Cull())
            {
                return;
            }

            Setup();
            _lighting.Setup(_context);
            _globalRamp.Setup(_context, globalRampSettings);

            DrawVisibleGeometry(settings);
            DrawUnsupportedShaders();
            DrawGizmos();

            Submit();
        }

        partial void PrepareBuffer();

        partial void PrepareForSceneWindow();

        private bool Cull()
        {
            if (!_camera.TryGetCullingParameters(out ScriptableCullingParameters parameters))
            {
                return false;
            }

            _cullingResults = _context.Cull(ref parameters);
            return true;
        }

        private void Setup()
        {
            _context.SetupCameraProperties(_camera);
            ClearRenderTargets();
            _buffer.BeginSample(_bufferName);
            ExecuteBuffer();
        }

        private void ClearRenderTargets()
        {
            const string sampleName = "Clear Render Targets";

            _buffer.BeginSample(sampleName);

            CameraClearFlags cameraClearFlags = _camera.clearFlags;
            bool clearDepth = cameraClearFlags <= CameraClearFlags.Depth;
            bool clearColor = cameraClearFlags == CameraClearFlags.Color;
            Color backgroundColor = clearColor ? _camera.backgroundColor.linear : Color.clear;
            _buffer.ClearRenderTarget(clearDepth, clearColor, backgroundColor);

            _buffer.EndSample(sampleName);
        }

        private void Submit()
        {
            _buffer.EndSample(_bufferName);
            ExecuteBuffer();
            _context.Submit();
        }

        private void ExecuteBuffer()
        {
            _context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
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