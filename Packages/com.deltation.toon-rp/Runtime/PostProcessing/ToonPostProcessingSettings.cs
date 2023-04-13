using System;

namespace DELTation.ToonRP.PostProcessing
{
    [Serializable]
    public struct ToonPostProcessingSettings
    {
        public bool Enabled;
        public ToonBloomSettings Bloom;
        public ToonOutlineSettings Outline;
    }
}