using System;
using UnityEngine;
using static DELTation.ToonRP.PostProcessing.BuiltIn.ToonDebugPassSettings;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    [CreateAssetMenu(menuName = Path + "Debug")]
    public class ToonDebugPassAsset : ToonPostProcessingPassAsset<ToonDebugPassSettings>
    {
        private void Reset()
        {
            Settings.MotionVectors = new MotionVectorsSettings
            {
                Scale = 10f,
                SceneIntensity = 0.5f,
            };
        }

        public override int Order() => ToonPostProcessingPassOrders.Debug;

        public override IToonPostProcessingPass CreatePass() => new ToonDebugPass();

        protected override string[] ForceIncludedShaderNames() => new[]
        {
            ToonDebugPass.ShaderName,
        };

        public override PrePassMode RequiredPrePassMode() =>
            Settings.Mode switch
            {
                DebugMode.None => PrePassMode.Off,
                DebugMode.TiledLighting => PrePassMode.Off,
                DebugMode.MotionVectors => PrePassMode.MotionVectors,
                DebugMode.Depth => PrePassMode.Depth,
                DebugMode.Normals => PrePassMode.Normals,
                _ => throw new ArgumentOutOfRangeException(),
            };
    }
}