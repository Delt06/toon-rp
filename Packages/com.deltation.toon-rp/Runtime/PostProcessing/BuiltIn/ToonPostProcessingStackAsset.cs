using System;
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
                Order = ToonPostProcessingStackSettings.PassOrder.PostUpscale,
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
                Vignette = new ToonVignetteSettings
                {
                    CenterX = 0.5f,
                    CenterY = 0.5f,
                    Intensity = 1.0f,
                    Roundness = 0.5f,
                    Smoothness = 0.6f,
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

        public override int Order() => Settings.Order switch
        {
            ToonPostProcessingStackSettings.PassOrder.PreUpscale =>
                ToonPostProcessingPassOrders.PostProcessingStackPreUpscale,
            ToonPostProcessingStackSettings.PassOrder.PostUpscale =>
                ToonPostProcessingPassOrders.PostProcessingStackPostUpscale,
            _ => throw new ArgumentOutOfRangeException(),
        };

        public override IToonPostProcessingPass CreatePass() => new ToonPostProcessingStack();

        protected override string[] ForceIncludedShaderNames() => new[]
        {
            ToonPostProcessingStack.ShaderName,
        };
    }
}