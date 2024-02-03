using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    public class ToonPostProcessingStack : ToonPostProcessingPassBase
    {
        public const string ShaderName = "Hidden/Toon RP/Post-Processing Stack";
        public const string FxaaLowKeywordName = "_FXAA_LOW";
        public const string FxaaHighKeywordName = "_FXAA_HIGH";
        public const string ToneMappingKeywordName = "_TONE_MAPPING";
        public const string VignetteKeywordName = "_VIGNETTE";
        public const string LookupTableKeywordName = "_LOOKUP_TABLE";
        public const string FilmGrainKeywordName = "_FILM_GRAIN";

        private static readonly int FxaaFixedContrastThresholdId = Shader.PropertyToID("_FXAA_FixedContrastThreshold");
        private static readonly int FxaaRelativeContrastThresholdId =
            Shader.PropertyToID("_FXAA_RelativeContrastThreshold");
        private static readonly int FxaaSubpixelBlendingFactorId = Shader.PropertyToID("_FXAA_SubpixelBlendingFactor");

        private static readonly int ToneMappingExposureId = Shader.PropertyToID("_ToneMapping_Exposure");

        private static readonly int VignetteCenterId = Shader.PropertyToID("_Vignette_Center");
        private static readonly int VignetteIntensityId = Shader.PropertyToID("_Vignette_Intensity");
        private static readonly int VignetteSmoothnessId = Shader.PropertyToID("_Vignette_Smoothness");
        private static readonly int VignetteRoundnessId = Shader.PropertyToID("_Vignette_Roundness");
        private static readonly int VignetteColorId = Shader.PropertyToID("_Vignette_Color");

        private static readonly int LookupTableTextureId = Shader.PropertyToID("_LookupTable_Texture");

        private static readonly int FilmGrainTextureId = Shader.PropertyToID("_FilmGrain_Texture");
        private static readonly int FilmGrainIntensityId = Shader.PropertyToID("_FilmGrain_Intensity");
        private static readonly int FilmGrainLuminanceThreshold0Id =
            Shader.PropertyToID("_FilmGrain_LuminanceThreshold0");
        private static readonly int FilmGrainLuminanceThreshold1Id =
            Shader.PropertyToID("_FilmGrain_LuminanceThreshold1");
        private readonly ToonPipelineMaterial _material = new(ShaderName, "Toon RP Post-Processing Stack");

        private ToonPostProcessingStackSettings _stackSettings;

        public override void Dispose()
        {
            base.Dispose();
            _material.Dispose();
        }

        public override bool IsEnabled(in ToonPostProcessingSettings settings)
        {
            ToonPostProcessingStackSettings stackSettings = settings.Find<ToonPostProcessingStackSettings>();
            return stackSettings.Fxaa.Enabled ||
                   stackSettings.ToneMapping.Enabled ||
                   stackSettings.Vignette.Enabled ||
                   stackSettings.LookupTable.Enabled ||
                   stackSettings.FilmGrain.Enabled
                ;
        }

        public override void Setup(CommandBuffer cmd, in ToonPostProcessingContext context)
        {
            base.Setup(cmd, in context);
            _stackSettings = context.Settings.Find<ToonPostProcessingStackSettings>();
        }

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination, bool destinationIsIntermediateTexture)
        {
            Material material = _material.GetOrCreate();

            HandleFxaaProperties(material, _stackSettings.Fxaa);
            HandleToneMappingProperties(material, _stackSettings.ToneMapping);
            HandleVignetteProperties(material, _stackSettings.Vignette);
            HandleLookupTextureProperties(material, _stackSettings.LookupTable);
            HandleFilmGrainProperties(material, _stackSettings.FilmGrain);

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.PostProcessingStack)))
            {
                cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmd.SetGlobalTexture(ToonBlitter.MainTexId, source);
                ToonBlitter.Blit(cmd, material, destinationIsIntermediateTexture, 0);
            }
        }

        private static void HandleFxaaProperties(Material material, in ToonFxaaSettings fxaaSettings)
        {
            if (fxaaSettings is { Enabled: true, HighQuality: true })
            {
                material.SetFloat(FxaaFixedContrastThresholdId, fxaaSettings.FixedContrastThresholdId);
                material.SetFloat(FxaaRelativeContrastThresholdId, fxaaSettings.RelativeContrastThreshold);
                material.SetFloat(FxaaSubpixelBlendingFactorId, fxaaSettings.SubpixelBlendingFactor);
            }

            (bool fxaaLow, bool fxaaHigh) = (fxaaSettings.Enabled, fxaaSettings.HighQuality) switch
            {
                (false, var _) => (false, false),
                (true, false) => (true, false),
                (true, true) => (false, true),
            };
            material.SetKeyword(FxaaLowKeywordName, fxaaLow);
            material.SetKeyword(FxaaHighKeywordName, fxaaHigh);
        }

        private static void HandleToneMappingProperties(Material material,
            in ToonToneMappingSettings toneMappingSettings)
        {
            material.SetKeyword(ToneMappingKeywordName, toneMappingSettings.Enabled);
            if (toneMappingSettings.Enabled)
            {
                material.SetFloat(ToneMappingExposureId, toneMappingSettings.Exposure);
            }
        }

        private static void HandleVignetteProperties(Material material, in ToonVignetteSettings vignetteSettings)
        {
            material.SetKeyword(VignetteKeywordName, vignetteSettings.Enabled);
            if (!vignetteSettings.Enabled)
            {
                return;
            }

            material.SetVector(VignetteCenterId, new Vector4(vignetteSettings.CenterX, vignetteSettings.CenterY));
            material.SetFloat(VignetteIntensityId, vignetteSettings.Intensity);
            material.SetFloat(VignetteSmoothnessId, 1.0f / vignetteSettings.Roundness);
            material.SetFloat(VignetteRoundnessId, 1.0f / vignetteSettings.Smoothness);
            material.SetColor(VignetteColorId, vignetteSettings.Color);
        }

        private static void HandleLookupTextureProperties(Material material,
            in ToonLookupTableSettings lookupTableSettings)
        {
            material.SetKeyword(LookupTableKeywordName, lookupTableSettings.Enabled);
            material.SetTexture(LookupTableTextureId, lookupTableSettings.Enabled ? lookupTableSettings.Texture : null);
        }

        private static void HandleFilmGrainProperties(Material material, in ToonFilmGrainSettings filmGrainSettings)
        {
            material.SetKeyword(FilmGrainKeywordName, filmGrainSettings.Enabled);
            Texture2D texture = filmGrainSettings.Enabled ? filmGrainSettings.Texture : null;
            material.SetTexture(FilmGrainTextureId, texture);

            if (!filmGrainSettings.Enabled)
            {
                return;
            }

            material.SetFloat(FilmGrainIntensityId, filmGrainSettings.Intensity);

            float threshold0 = filmGrainSettings.LuminanceThreshold * 0.5f;
            float threshold1 = filmGrainSettings.LuminanceThreshold;
            material.SetFloat(FilmGrainLuminanceThreshold0Id, threshold0);
            material.SetFloat(FilmGrainLuminanceThreshold1Id, threshold1);
        }
    }
}