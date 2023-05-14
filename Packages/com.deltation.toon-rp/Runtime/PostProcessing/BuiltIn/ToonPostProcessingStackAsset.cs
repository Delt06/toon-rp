using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [CreateAssetMenu(menuName = Path + "Post-Processing Stack")]
    public class ToonPostProcessingStackAsset : ToonPostProcessingPassAsset<ToonPostProcessingStackSettings>
    {
        private void Reset()
        {
            Settings = new ToonPostProcessingStackSettings
            {
                Fxaa = new ToonFxaaSettings
                {
                    HighQuality = true,
                    FixedContrastThresholdId = 0.0833f,
                    RelativeContrastThreshold = 0.166f,
                    SubpixelBlendingFactor = 0.75f,
                },
            };
        }

        public override int Order() => ToonPostProcessingPassOrders.PostProcessingStack;

        public override IToonPostProcessingPass CreatePass() => new ToonPostProcessingStack();

        protected override string[] ForceIncludedShaderNames() => new[]
        {
            ToonPostProcessingStack.ShaderName,
        };
    }
}