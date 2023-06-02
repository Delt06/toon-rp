using DELTation.ToonRP.PostProcessing.BuiltIn;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    public class ToonScreenSpaceOutlineAfterOpaque : ToonRenderingExtensionBase
    {
        private readonly ToonScreenSpaceOutlineImpl _impl = new();
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
            _rtWidth = context.CameraRenderTarget.Width;
            _rtHeight = context.CameraRenderTarget.Height;
        }

        public override void Render()
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.ScreenSpaceOutlinesAfterOpaque)))
            {
                _impl.EnableAlphaBlending(true);
                _impl.SetRtSize(_rtWidth, _rtHeight);
                _impl.RenderViaCustomBlit(cmd, _settings);
            }

            _context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }
}