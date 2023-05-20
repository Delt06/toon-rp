namespace DELTation.ToonRP.Extensions
{
    public abstract class ToonRenderingExtensionBase : IToonRenderingExtension
    {
        public abstract void Render();

        public virtual void Setup(in ToonRenderingExtensionContext context,
            IToonRenderingExtensionSettingsStorage settingsStorage) { }

        public virtual void Cleanup() { }
    }
}