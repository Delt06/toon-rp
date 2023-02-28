using System;

namespace ToonRP.Runtime
{
    [Serializable]
    public struct ToonCameraRendererSettings
    {
        public enum DepthPrePassMode
        {
            Off,
            Depth,
            DepthNormals,
        }

        public enum MsaaMode
        {
            Off = 1,
            _2x = 2,
            _4x = 4,
            _8x = 8,
        }

        public bool AllowHdr;
        public MsaaMode Msaa;
        public bool MsaaResolveDepth;
        public DepthPrePassMode DepthPrePass;

        public bool UseSrpBatching;
        public bool UseDynamicBatching;
    }
}