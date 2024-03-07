using DELTation.ToonRP.PostProcessing.BuiltIn;
using UnityEngine;
using static DELTation.ToonRP.Extensions.BuiltIn.ToonScreenSpaceOutlineAfterOpaqueSettings;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    [CreateAssetMenu(menuName = Path + "Screen-Space Outline (After Opaque)")]
    public class
        ToonScreenSpaceOutlineAfterOpaqueAsset : ToonRenderingExtensionAsset<ToonScreenSpaceOutlineAfterOpaqueSettings>
    {
        private const ToonRenderingEvent RenderingEvent = ToonRenderingEvent.AfterOpaque;

        private void Reset()
        {
            Settings = new ToonScreenSpaceOutlineAfterOpaqueSettings
            {
                Color = Color.black,
                DepthFilter = new ToonScreenSpaceOutlineSettings.OutlineFilter
                {
                    Enabled = true,
                    Threshold = 1.0f,
                    Smoothness = 0.5f,
                },
                NormalsFilter = new ToonScreenSpaceOutlineSettings.OutlineFilter
                {
                    Enabled = true,
                    Smoothness = 5.0f,
                    Threshold = 0.5f,
                },
                UseFog = true,
                MaxDistance = 100.0f,
                DistanceFade = 0.1f,
            };
        }

        public override bool UsesRenderingEvent(ToonRenderingEvent renderingEvent) => renderingEvent == RenderingEvent;

        public override IToonRenderingExtension CreateExtensionOrDefault(ToonRenderingEvent renderingEvent) =>
            RenderingEvent == renderingEvent ? new ToonScreenSpaceOutlineAfterOpaque() : null;

        protected override string[] ForceIncludedShaderNames() => new[]
        {
            ToonScreenSpaceOutlineImpl.ShaderName,
        };

        public override ToonPrePassRequirement RequiredPrePassMode() =>
            new(ToonScreenSpaceOutline.RequiredPrePassMode(ConvertToCommonSettings(Settings)), RenderingEvent);
    }
}