using System;
using DELTation.ToonRP.Attributes;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [Serializable]
    public struct ToonPostProcessingStackSettings
    {
        private const int HeaderSize = 12;

        [ToonRpHeader("FXAA", Size = HeaderSize)]
        public ToonFxaaSettings Fxaa;
    }
}