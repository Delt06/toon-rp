namespace DELTation.ToonRP
{
    public enum ToonRenderingEvent
    {
        BeforeOpaque = 0,
        AfterOpaque,
        BeforeSkybox,
        AfterSkybox,
        BeforeTransparent,
        AfterTransparent,
        BeforePostProcessing,
        AfterPostProcessing,
    }
}