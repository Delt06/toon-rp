using System;

namespace DELTation.ToonRP
{
    public enum ToonRenderingEvent
    {
        BeforePrepass = 0,
        AfterPrepass = 1,
        BeforeOpaque = 2,
        AfterOpaque = 3,
        BeforeSkybox = 4,
        AfterSkybox = 5,
        BeforeTransparent = 6,
        AfterTransparent = 7,
        BeforePostProcessing = 8,
        AfterPostProcessing = 9,

        BeforeGeometryPasses = 10,
    }

    internal static class ToonRenderingEvents
    {
        public static readonly ToonRenderingEvent[] All =
            (ToonRenderingEvent[]) Enum.GetValues(typeof(ToonRenderingEvent));
    }
}