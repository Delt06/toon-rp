using DELTation.ToonRP.PostProcessing.BuiltIn;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    public class ToonScreenSpaceOutlineAfterOpaque : ToonRenderingExtensionBase
    {
        private readonly ToonScreenSpaceOutlineImpl _impl = new();
        private Camera _camera;
        private ToonCameraRenderTarget _cameraRenderTarget;
        private ScriptableRenderContext _context;
        private int _rtHeight;
        private int _rtWidth;
        private ToonScreenSpaceOutlineSettings _settings;

        public override bool ShouldRender(in ToonRenderingExtensionContext context) => IsGameOrSceneView(context);

        public override void Setup(in ToonRenderingExtensionContext context,
            IToonRenderingExtensionSettingsStorage settingsStorage)
        {
            base.Setup(context, settingsStorage);
            _context = context.ScriptableRenderContext;
            _settings = ToonScreenSpaceOutlineAfterOpaqueSettings.ConvertToCommonSettings(
                settingsStorage.GetSettings<ToonScreenSpaceOutlineAfterOpaqueSettings>(this)
            );
            _cameraRenderTarget = context.CameraRenderTarget;
            _rtWidth = _cameraRenderTarget.Width;
            _rtHeight = _cameraRenderTarget.Height;
            _camera = context.Camera;
        }

        public override void Render()
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.ScreenSpaceOutlinesAfterOpaque)))
            {
                _impl.EnableAlphaBlending(true);
                _impl.SetRtSize(_rtWidth, _rtHeight);
                bool renderToTexture = _cameraRenderTarget.RenderToTexture || _camera.targetTexture != null;
                _impl.RenderViaBlit(cmd, _settings, renderToTexture);
            }

            _context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }
}