using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [CreateAssetMenu(menuName = Path + "Debug")]
    public class ToonDebugPassAsset : ToonPostProcessingPassAsset<ToonDebugPassSettings>
    {
        private void Reset()
        {
            Settings.TiledLighting.ShowOpaque = true;
        }

        public override int Order() => ToonPostProcessingPassOrders.Debug;

        public override IToonPostProcessingPass CreatePass() => new ToonDebugPass();

        protected override string[] ForceIncludedShaderNames() => new[]
        {
            ToonDebugPass.ShaderName,
        };
    }
}