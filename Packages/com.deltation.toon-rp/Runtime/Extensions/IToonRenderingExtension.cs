namespace DELTation.ToonRP.Extensions
{
    public interface IToonRenderingExtension
    {
        void Render(in ToonRenderingExtensionContext context);
        void Setup(in ToonRenderingExtensionContext context, IToonRenderingExtensionSettingsStorage settingsStorage);
        void Cleanup(in ToonRenderingExtensionContext context);
    }
}