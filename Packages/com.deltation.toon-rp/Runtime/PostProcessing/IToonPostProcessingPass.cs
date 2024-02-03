using System;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing
{
    public interface IToonPostProcessingPass : IDisposable
    {
        int Order { get; set; }

        /// <summary>
        ///     Given the current settings, whether the pass should run.
        /// </summary>
        /// <param name="settings">Post-processing settings.</param>
        /// <returns></returns>
        bool IsEnabled(in ToonPostProcessingSettings settings);

        /// <summary>
        ///     Returns true if the pass requires separate source and destination.
        ///     Returns false if it can render the result back to the source RT.
        /// </summary>
        /// <returns>True if source and destination need to be distinct.</returns>
        bool NeedsDistinctSourceAndDestination();

        void Setup(CommandBuffer cmd, in ToonPostProcessingContext context);

        void Render(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination, bool destinationIsIntermediateTexture);

        void Cleanup(CommandBuffer cmd);
        bool InterruptsGeometryRenderPass(in ToonPostProcessingContext context);
    }
}