using System;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [Serializable]
    public struct ToonPostProcessingStackSettings
    {
        [ToonRpHeader("FXAA")]
        public ToonFxaaSettings Fxaa;
    }
}