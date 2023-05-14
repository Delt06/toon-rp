using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    public class ToonPostProcessingStack : ToonPostProcessingPassBase
    {
        public const string ShaderName = "Hidden/Toon RP/Post-Processing Stack";

        private static readonly int FxaaFixedContrastThresholdId = Shader.PropertyToID("_FXAA_FixedContrastThreshold");
        private static readonly int FxaaRelativeContrastThresholdId =
            Shader.PropertyToID("_FXAA_RelativeContrastThreshold");
        private static readonly int FxaaSubpixelBlendingFactorId = Shader.PropertyToID("_FXAA_SubpixelBlendingFactor");
        private static readonly int FilmGrainTextureId = Shader.PropertyToID("_FilmGrain_Texture");
        private static readonly int FilmGrainIntensityId = Shader.PropertyToID("_FilmGrain_Intensity");
        private static readonly int FilmGrainLuminanceThreshold0Id =
            Shader.PropertyToID("_FilmGrain_LuminanceThreshold0");
        private static readonly int FilmGrainLuminanceThreshold1Id =
            Shader.PropertyToID("_FilmGrain_LuminanceThreshold1");
        private ToonFilmGrainSettings _filmGrainSettings;
        private ToonFxaaSettings _fxaaSettings;
        private Material _material;

        private void EnsureMaterialIsCreated()
        {
            if (_material != null)
            {
                return;
            }

            _material = new Material(Shader.Find(ShaderName))
            {
                name = "Toon RP Post-Processing Stack",
            };
        }

        public override bool IsEnabled(in ToonPostProcessingSettings settings)
        {
            ToonPostProcessingStackSettings stackSettings = settings.Find<ToonPostProcessingStackSettings>();
            return stackSettings.Fxaa.Enabled ||
                   stackSettings.FilmGrain.Enabled
                ;
        }

        public override void Setup(CommandBuffer cmd, in ToonPostProcessingContext context)
        {
            base.Setup(cmd, in context);
            ToonPostProcessingStackSettings stackSettings = context.Settings.Find<ToonPostProcessingStackSettings>();
            _fxaaSettings = stackSettings.Fxaa;
            _filmGrainSettings = stackSettings.FilmGrain;
        }

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination)
        {
            EnsureMaterialIsCreated();

            HandleFxaaProperties();
            HandleFilmGrainProperties();

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.PostProcessingStack)))
            {
                cmd.Blit(source, destination, _material);
            }
        }

        private void HandleFxaaProperties()
        {
            if (_fxaaSettings.Enabled && _fxaaSettings.HighQuality)
            {
                _material.SetFloat(FxaaFixedContrastThresholdId, _fxaaSettings.FixedContrastThresholdId);
                _material.SetFloat(FxaaRelativeContrastThresholdId, _fxaaSettings.RelativeContrastThreshold);
                _material.SetFloat(FxaaSubpixelBlendingFactorId, _fxaaSettings.SubpixelBlendingFactor);
            }

            (bool fxaaLow, bool fxaaHigh) = (_fxaaSettings.Enabled, _fxaaSettings.HighQuality) switch
            {
                (false, var _) => (false, false),
                (true, false) => (true, false),
                (true, true) => (false, true),
            };
            _material.SetKeyword("_FXAA_LOW", fxaaLow);
            _material.SetKeyword("_FXAA_HIGH", fxaaHigh);
        }

        private void HandleFilmGrainProperties()
        {
            _material.SetKeyword("_FILM_GRAIN", _filmGrainSettings.Enabled);
            Texture2D texture = _filmGrainSettings.Enabled ? _filmGrainSettings.Texture : null;
            _material.SetTexture(FilmGrainTextureId, texture);

            if (!_filmGrainSettings.Enabled)
            {
                return;
            }

            _material.SetFloat(FilmGrainIntensityId, _filmGrainSettings.Intensity);

            float threshold0 = _filmGrainSettings.LuminanceThreshold * 0.5f;
            float threshold1 = _filmGrainSettings.LuminanceThreshold;
            _material.SetFloat(FilmGrainLuminanceThreshold0Id, threshold0);
            _material.SetFloat(FilmGrainLuminanceThreshold1Id, threshold1);
        }
    }
}