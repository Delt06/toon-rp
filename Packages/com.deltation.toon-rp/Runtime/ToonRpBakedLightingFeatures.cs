using System;

namespace DELTation.ToonRP
{
    [Flags]
    public enum ToonRpBakedLightingFeatures
    {
        None = 0,
        LightProbes = 1 << 0,
        LightMaps = 1 << 1,
        ShadowMask = LightMaps | 1 << 2,
        Everything = LightProbes | LightMaps | ShadowMask,
    }
}