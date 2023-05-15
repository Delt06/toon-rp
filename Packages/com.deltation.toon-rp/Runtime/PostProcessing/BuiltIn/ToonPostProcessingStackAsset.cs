using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

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
                ToneMapping = new ToonToneMappingSettings
                {
                    Exposure = 1.5f,
                },
                FilmGrain = new ToonFilmGrainSettings
                {
                    Intensity = 0.01f,
                    LuminanceThreshold = 1.0f,
                },
            };

#if UNITY_EDITOR
            Settings.LookupTable.Texture =
                AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.deltation.toon-rp/Assets/DefaultLUT.png");

            Settings.FilmGrain.Texture =
                AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.deltation.toon-rp/Assets/FilmGrain.png");
#endif // UNITY_EDITOR
        }

        public override int Order() => ToonPostProcessingPassOrders.PostProcessingStack;

        public override IToonPostProcessingPass CreatePass() => new ToonPostProcessingStack();

        protected override string[] ForceIncludedShaderNames() => new[]
        {
            ToonPostProcessingStack.ShaderName,
        };
    }
}