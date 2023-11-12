using UnityEngine.Rendering;

namespace DELTation.ToonRP.Extensions
{
    public interface IToonRenderingExtension
    {
        bool ShouldRender(in ToonRenderingExtensionContext context);

        void Setup(in ToonRenderingExtensionContext context, IToonRenderingExtensionSettingsStorage settingsStorage);
        void Render();
        void Cleanup();

        void OnPrePass(PrePassMode prePassMode, ref ScriptableRenderContext context,
            CommandBuffer cmd,
            ref DrawingSettings drawingSettings, ref FilteringSettings filteringSettings,
            ref RenderStateBlock renderStateBlock);

        bool RequireCameraDepthStore(in ToonRenderingExtensionContext context);
    }
}