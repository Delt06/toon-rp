using System;

namespace DELTation.ToonRP
{
    public enum ToonRenderingEvent
    {
        BeforePrepass = 0,
        AfterPrepass = 1,

        BeforeGeometryPasses = 9,
        BeforeOpaque = 10,
        AfterOpaque = 11,
        BeforeSkybox = 12,
        AfterSkybox = 13,

        BeforeTransparent = 20,
        AfterTransparent = 21,

        BeforePostProcessing = 50,
        AfterPostProcessing = 55,

        InvalidLatest = int.MaxValue,
    }

    internal static class ToonRenderingEvents
    {
        public static readonly ToonRenderingEvent[] All =
            (ToonRenderingEvent[]) Enum.GetValues(typeof(ToonRenderingEvent));
    }
}