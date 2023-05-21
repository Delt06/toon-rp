using DELTation.ToonRP.PostProcessing.BuiltIn;
using UnityEngine;
using static DELTation.ToonRP.Extensions.BuiltIn.ToonScreenSpaceOutlineAfterOpaqueSettings;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    [CreateAssetMenu(menuName = Path + "Screen-Space Outline (After Opaque)")]
    public class
        ToonScreenSpaceOutlineAfterOpaqueAsset : ToonRenderingExtensionAsset<ToonScreenSpaceOutlineAfterOpaqueSettings>
    {
        public override ToonRenderingEvent Event => ToonRenderingEvent.AfterOpaque;

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

        public override IToonRenderingExtension CreateExtension() => new ToonScreenSpaceOutlineAfterOpaque();

        protected override string[] ForceIncludedShaderNames() => new[]
        {
            ToonScreenSpaceOutlineImpl.ShaderName,
        };

        public override DepthPrePassMode RequiredDepthPrePassMode() =>
            ToonScreenSpaceOutlineAsset.RequiredDepthPrePassMode(ConvertToCommonSettings(Settings));
    }
}