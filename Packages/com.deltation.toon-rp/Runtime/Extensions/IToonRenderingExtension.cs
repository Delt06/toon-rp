namespace DELTation.ToonRP.Extensions
{
    public interface IToonRenderingExtension
    {
        void Render(in ToonRenderingExtensionContext context);
        void Setup(in ToonRenderingExtensionContext context);
        void Cleanup(in ToonRenderingExtensionContext context);
    }
}