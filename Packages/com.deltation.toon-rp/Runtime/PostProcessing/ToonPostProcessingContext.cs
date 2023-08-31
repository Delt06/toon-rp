using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace DELTation.ToonRP.PostProcessing
{
    public struct ToonPostProcessingContext
    {
        public ToonPostProcessingSettings Settings;
        public GraphicsFormat ColorFormat;
        public int RtWidth;
        public int RtHeight;
        public Camera Camera;
    }
}