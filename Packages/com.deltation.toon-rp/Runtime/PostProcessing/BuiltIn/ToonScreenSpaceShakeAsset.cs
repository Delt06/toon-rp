using System;
using UnityEngine;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [CreateAssetMenu(menuName = Path + "Screen-Space Shake")]
    public class ToonScreenSpaceShakeAsset : ToonPostProcessingPassAsset<ToonScreenSpaceShakeSettings>
    {
        private void Reset()
        {
            Settings = new ToonScreenSpaceShakeSettings
            {
                Order = ToonPostProcessingPassOrder.PostUpscale,
            };
        }

        public override int Order() => Settings.Order switch
        {
            ToonPostProcessingPassOrder.PreUpscale =>
                ToonPostProcessingPassOrders.PostProcessingStackPreUpscale,
            ToonPostProcessingPassOrder.PostUpscale =>
                ToonPostProcessingPassOrders.PostProcessingStackPostUpscale,
            _ => throw new ArgumentOutOfRangeException(),
        };

        public override IToonPostProcessingPass CreatePass() => new ToonScreenSpaceShake();

        protected override string[] ForceIncludedShaderNames() => new[]
        {
            ToonScreenSpaceShake.ShaderName,
        };
    }
}