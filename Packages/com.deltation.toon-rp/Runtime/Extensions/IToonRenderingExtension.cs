namespace DELTation.ToonRP.Extensions
{
    public interface IToonRenderingExtension
    {
        bool ShouldRender(in ToonRenderingExtensionContext context);

        void Setup(in ToonRenderingExtensionContext context, IToonRenderingExtensionSettingsStorage settingsStorage);
        void Render();
        void Cleanup();
    }
}