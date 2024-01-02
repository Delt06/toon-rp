using System;
using JetBrains.Annotations;

namespace DELTation.ToonRP.Extensions
{
    public interface IToonRenderingExtensionSettingsStorage : IDisposable
    {
        TSettings GetSettings<TSettings>([NotNull] IToonRenderingExtension extension);
    }
}