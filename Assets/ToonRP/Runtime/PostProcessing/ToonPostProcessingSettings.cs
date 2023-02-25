using System;

namespace ToonRP.Runtime.PostProcessing
{
    [Serializable]
    public struct ToonPostProcessingSettings
    {
        public bool Enabled;
        public ToonBloomSettings Bloom;
        public ToonOutlineSettings Outline;
    }
}