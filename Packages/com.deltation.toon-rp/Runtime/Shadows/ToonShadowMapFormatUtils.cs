using System;
using UnityEngine.Device;
using UnityEngine.Experimental.Rendering;
using static DELTation.ToonRP.Shadows.ToonShadowMapsSettings;

namespace DELTation.ToonRP.Shadows
{
    internal static class ToonShadowMapFormatUtils
    {
        private static readonly ShadowMapBits[] All =
            (ShadowMapBits[]) Enum.GetValues(typeof(ShadowMapBits));

        public static (GraphicsFormat format, int bits) GetSupportedShadowMapFormat(ShadowMapBits desiredBits)
        {
            for (int index = All.Length - 1; index >= 0; --index)
            {
                if (All[index] > desiredBits)
                {
                    continue;
                }

                ShadowMapBits shadowMapBits = All[index];

                const int stencilBits = 0;
                GraphicsFormat format = GraphicsFormatUtility.GetDepthStencilFormat((int) shadowMapBits, stencilBits);
                if (SystemInfo.IsFormatSupported(format, FormatUsage.Render | FormatUsage.Sample))
                {
                    return (format, (int) shadowMapBits);
                }
            }

            GraphicsFormat defaultFormat = UnityEngine.SystemInfo.GetGraphicsFormat(DefaultFormat.Shadow);
            int defaultBits = GraphicsFormatUtility.GetDepthBits(defaultFormat);
            return (defaultFormat, defaultBits);
        }

        public static GraphicsFormat GetSupportedVsmTextureFormat(VsmTexturePrecision desiredPrecision)
        {
            // R - depth, G - depth^2
            const GraphicsFormat floatFormat = GraphicsFormat.R32G32_SFloat;
            const GraphicsFormat halfFormat = GraphicsFormat.R16G16_SFloat;

            GraphicsFormat desiredFormat = desiredPrecision switch
            {
                VsmTexturePrecision.Float => floatFormat,
                VsmTexturePrecision.Half => halfFormat,
                var _ => floatFormat,
            };

            return UnityEngine.SystemInfo.IsFormatSupported(desiredFormat,
                FormatUsage.Render | FormatUsage.Sample | FormatUsage.Linear
            )
                ? desiredFormat
                : halfFormat;
        }
    }
}