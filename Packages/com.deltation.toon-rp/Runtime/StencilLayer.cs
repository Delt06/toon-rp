using System;

namespace DELTation.ToonRP
{
    public enum StencilLayer
    {
        None,
        _0,
        _1,
        _2,
        _3,
        _4,
        _5,
        _6,
        _7,
    }
    
    public enum StencilPreset
    {
        None,
        _0,
        _1,
        _2,
        _3,
        _4,
        _5,
        _6,
        _7,
        Custom,
    }

    public static class StencilLayerExt
    {
        public static byte ToReference(this StencilLayer layer)
        {
            int bit = layer switch
            {
                StencilLayer.None => throw new ArgumentOutOfRangeException(nameof(layer), layer, null),
                StencilLayer._0 => 0,
                StencilLayer._1 => 1,
                StencilLayer._2 => 2,
                StencilLayer._3 => 3,
                StencilLayer._4 => 4,
                StencilLayer._5 => 5,
                StencilLayer._6 => 6,
                StencilLayer._7 => 7,
                _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, null),
            };
            return (byte) (1 << bit);
        }
    }
}