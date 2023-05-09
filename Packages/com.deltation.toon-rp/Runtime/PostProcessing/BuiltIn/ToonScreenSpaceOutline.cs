using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    public class ToonScreenSpaceOutline : ToonPostProcessingPassBase
    {
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int ColorRampId = Shader.PropertyToID("_ColorRamp");
        private static readonly int DepthRampId = Shader.PropertyToID("_DepthRamp");
        private static readonly int NormalsRampId = Shader.PropertyToID("_NormalsRamp");
        private static readonly int DistanceFadeId = Shader.PropertyToID("_DistanceFade");

        private Material _material;
        private ToonScreenSpaceOutlineSettings _settings;
        private Shader _shader;

        public override bool IsEnabled(in ToonPostProcessingSettings settings) =>
            settings.Outline.Mode == ToonOutlineSettings.OutlineMode.ScreenSpace;

        private void EnsureMaterialIsCreated()
        {
            if (_material != null && _shader != null)
            {
                return;
            }

            _shader = Shader.Find("Hidden/Toon RP/Outline (Screen Space)");
            _material = new Material(_shader)
            {
                name = "Toon RP Outline (Screen Space)",
            };
        }

        public override void Setup(CommandBuffer cmd, in ToonPostProcessingContext context)
        {
            base.Setup(cmd, in context);
            _settings = context.Settings.Outline.ScreenSpace;
        }

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination)
        {
            EnsureMaterialIsCreated();
            UpdateMaterial();

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.ScreenSpaceOutlines)))
            {
                cmd.Blit(source, destination, _material);
            }
        }

        private void UpdateMaterial()
        {
            _material.SetVector(OutlineColorId, _settings.Color);

            UpdateMaterialFilter(_settings.ColorFilter, ColorRampId, "_COLOR");
            UpdateMaterialFilter(_settings.NormalsFilter, NormalsRampId, "_NORMALS");
            UpdateMaterialFilter(_settings.DepthFilter, DepthRampId, "_DEPTH");

            _material.SetKeyword(new LocalKeyword(_shader, "_USE_FOG"), _settings.UseFog);

            _material.SetVector(DistanceFadeId,
                new Vector4(
                    1.0f / _settings.MaxDistance,
                    1.0f / _settings.DistanceFade
                )
            );
        }

        private void UpdateMaterialFilter(in ToonScreenSpaceOutlineSettings.OutlineFilter filter, int rampId,
            string keyword)
        {
            var ramp = new Vector4(filter.Threshold, filter.Threshold + filter.Smoothness);
            _material.SetVector(rampId, ramp);
            _material.SetKeyword(new LocalKeyword(_shader, keyword), filter.Enabled);
        }
    }
}