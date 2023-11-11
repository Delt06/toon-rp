using UnityEngine.Experimental.Rendering;

namespace DELTation.ToonRP
{
    public static class ToonFormatUtils
    {
        public const GraphicsFormat DefaultDepthFormat = GraphicsFormat.D16_UNorm;
        public const GraphicsFormat DefaultDepthStencilFormat = GraphicsFormat.D16_UNorm;

        public static GraphicsFormat GetDefaultDepthFormat(bool stencil) =>
            stencil ? DefaultDepthStencilFormat : DefaultDepthFormat;
    }
}