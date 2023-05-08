using System;

namespace DELTation.ToonRP.PostProcessing
{
    [Serializable]
    public struct ToonPostProcessingSettings
    {
        public bool Enabled;
        public ToonFxaaSettings Fxaa;
        public ToonLightScatteringSettings LightScattering;
        public ToonBloomSettings Bloom;
        public ToonOutlineSettings Outline;
    }
}