﻿using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    public class ToonScreenSpaceOutline : ToonPostProcessingPassBase
    {
        private readonly ToonScreenSpaceOutlineImpl _impl = new();
        private ToonScreenSpaceOutlineSettings _settings;

        public override void Setup(CommandBuffer cmd, in ToonPostProcessingContext context)
        {
            base.Setup(cmd, in context);
            _settings = context.Settings.Find<ToonScreenSpaceOutlineSettings>();
        }

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination)
        {
            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.ScreenSpaceOutlines)))
            {
                _impl.RenderViaBlit(cmd, _settings, source, destination);
            }
        }

        public static PrePassMode RequiredPrePassMode(in ToonScreenSpaceOutlineSettings settings)
            => settings.NormalsFilter.Enabled
                ? PrePassMode.Normals | PrePassMode.Depth
                : PrePassMode.Depth;
    }
}