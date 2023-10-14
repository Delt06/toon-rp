using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [CreateAssetMenu(menuName = Path + "Screen-Space Outline")]
    public class ToonScreenSpaceOutlineAsset : ToonPostProcessingPassAsset<ToonScreenSpaceOutlineSettings>
    {
        private void Reset()
        {
            Settings = new ToonScreenSpaceOutlineSettings
            {
                Color = Color.black,
                ColorFilter = new ToonScreenSpaceOutlineSettings.OutlineFilter
                {
                    Enabled = false,
                    Threshold = 0.75f,
                    Smoothness = 0.5f,
                },
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

        public override PrePassMode RequiredPrePassMode() => ToonScreenSpaceOutline.RequiredPrePassMode(Settings);

        public override int Order() => ToonPostProcessingPassOrders.Outline;

        public override IToonPostProcessingPass CreatePass() => new ToonScreenSpaceOutline();

        protected override string[] ForceIncludedShaderNames() => new[]
        {
            ToonScreenSpaceOutlineImpl.ShaderName,
        };
    }
}