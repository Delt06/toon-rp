using UnityEngine;

namespace DELTation.ToonRP.PostProcessing
{
    public struct ToonPostProcessingContext
    {
        public ToonPostProcessingSettings Settings;
        public RenderTextureFormat ColorFormat;
        public int RtWidth;
        public int RtHeight;
        public Camera Camera;
    }
}