using System;
using DELTation.ToonRP.PostProcessing.BuiltIn;
using UnityEngine;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    [Serializable]
    public struct ToonScreenSpaceOutlineAfterOpaqueSettings
    {
        [ColorUsage(true, true)]
        public Color Color;

        public ToonScreenSpaceOutlineSettings.OutlineFilter DepthFilter;
        public ToonScreenSpaceOutlineSettings.OutlineFilter NormalsFilter;

        public bool UseFog;
        [Min(0f)]
        public float MaxDistance;
        [Range(0.001f, 1f)]
        public float DistanceFade;

        public static ToonScreenSpaceOutlineSettings ConvertToCommonSettings(
            in ToonScreenSpaceOutlineAfterOpaqueSettings settings) =>
            new()
            {
                Color = settings.Color,
                DepthFilter = settings.DepthFilter,
                NormalsFilter = settings.NormalsFilter,
                UseFog = settings.UseFog,
                DistanceFade = settings.DistanceFade,
                MaxDistance = settings.MaxDistance,
            };
    }
}