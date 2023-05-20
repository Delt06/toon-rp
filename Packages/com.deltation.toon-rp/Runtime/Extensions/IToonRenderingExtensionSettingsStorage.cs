using JetBrains.Annotations;

namespace DELTation.ToonRP.Extensions
{
    public interface IToonRenderingExtensionSettingsStorage
    {
        TSettings GetSettings<TSettings>([NotNull] IToonRenderingExtension extension);
    }
}