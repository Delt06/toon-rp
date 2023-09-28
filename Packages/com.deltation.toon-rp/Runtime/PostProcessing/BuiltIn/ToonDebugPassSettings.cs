using System;
using DELTation.ToonRP.Attributes;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [Serializable]
    public struct ToonDebugPassSettings
    {
        public enum DebugMode
        {
            None,
            TiledLighting,
        }

        public DebugMode Mode;

        [ToonRpShowIf(nameof(TiledLightingOn))]
        public TiledLightingSettings TiledLighting;

        private bool TiledLightingOn => Mode == DebugMode.TiledLighting;

        [Serializable]
        public struct TiledLightingSettings
        {
            public bool ShowOpaque;
            public bool ShowTransparent;
        }
    }
}