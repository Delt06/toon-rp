using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [CreateAssetMenu(menuName = Path + "Temporal AA")]
    public class ToonTemporalAAAsset : ToonPostProcessingPassAsset<ToonTemporalAASettings>
    {
        private void Reset()
        {
            Settings = new ToonTemporalAASettings
            {
                JitterScale = 1.0f,
                ModulationFactor = 0.8f,
            };
        }

        public override int Order() => ToonPostProcessingPassOrders.TemporalAA;

        public override IToonPostProcessingPass CreatePass() => new ToonTemporalAA();

        protected override string[] ForceIncludedShaderNames() => new[] { ToonTemporalAA.ShaderName };

        public override PrePassMode RequiredPrePassMode() => PrePassMode.MotionVectors;
    }
}