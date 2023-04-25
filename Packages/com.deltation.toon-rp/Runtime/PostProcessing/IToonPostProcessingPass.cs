using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing
{
    public interface IToonPostProcessingPass
    {
        bool IsEnabled(in ToonPostProcessingSettings settings);
        void Setup(CommandBuffer cmd, in ToonPostProcessingContext context);

        void Render(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination);

        void Cleanup(CommandBuffer cmd);
    }
}