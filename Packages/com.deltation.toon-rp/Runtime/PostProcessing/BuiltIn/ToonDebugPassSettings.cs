using System;

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
    }
}