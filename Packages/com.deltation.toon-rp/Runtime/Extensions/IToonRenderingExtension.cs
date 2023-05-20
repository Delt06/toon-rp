namespace DELTation.ToonRP.Extensions
{
    public interface IToonRenderingExtension
    {
        void Setup(in ToonRenderingExtensionContext context, IToonRenderingExtensionSettingsStorage settingsStorage);
        void Render();
        void Cleanup();
    }
}