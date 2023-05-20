namespace DELTation.ToonRP
{
    public enum ToonRenderingEvent
    {
        BeforeDepthPrepass,
        AfterDepthPrepass,
        BeforeOpaque,
        AfterOpaque,
        BeforeSkybox,
        AfterSkybox,
        BeforeTransparent,
        AfterTransparent,
        BeforePostProcessing,
        AfterPostProcessing,
    }
}