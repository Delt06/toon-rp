﻿using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing
{
    public abstract class ToonPostProcessingPassBase : IToonPostProcessingPass
    {
        protected ToonPostProcessingContext Context;

        public virtual bool IsEnabled(in ToonPostProcessingSettings settings) => true;
        public virtual bool NeedsDistinctSourceAndDestination() => true;

        public virtual void Setup(CommandBuffer cmd, in ToonPostProcessingContext context)
        {
            Context = context;
        }

        public abstract void Render(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination);

        public virtual void Cleanup(CommandBuffer cmd) { }

        public int Order { get; set; }

        public virtual void Dispose() { }

        public virtual bool RequireCameraDepthStore(in ToonPostProcessingContext context) => false;
    }
}