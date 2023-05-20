namespace DELTation.ToonRP.Extensions
{
    public abstract class ToonRenderingExtensionBase : IToonRenderingExtension
    {
        public abstract void Render(in ToonRenderingExtensionContext context);

        public virtual void Setup(in ToonRenderingExtensionContext context,
            IToonRenderingExtensionSettingsStorage settingsStorage) { }

        public virtual void Cleanup(in ToonRenderingExtensionContext context) { }
    }
}