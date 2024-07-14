using UnityEngine.Experimental.Rendering;

namespace DELTation.ToonRP
{
    public static class ToonFormatUtils
    {
        private const int DepthBits = 24;
        public static readonly GraphicsFormat DefaultDepthFormat =
            GraphicsFormatUtility.GetDepthStencilFormat(DepthBits, 0);
        public static readonly GraphicsFormat DefaultDepthStencilFormat =
            GraphicsFormatUtility.GetDepthStencilFormat(DepthBits, 8);

        public static GraphicsFormat GetDefaultDepthFormat(bool stencil) =>
            stencil ? DefaultDepthStencilFormat : DefaultDepthFormat;
    }
}