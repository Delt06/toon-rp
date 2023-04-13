using System;

namespace DELTation.ToonRP.PostProcessing
{
    [Serializable]
    public struct ToonOutlineSettings
    {
        public enum OutlineMode
        {
            Off,
            InvertedHull,
            ScreenSpace,
        }

        public OutlineMode Mode;
        public ToonInvertedHullOutlineSettings InvertedHull;
    }
}