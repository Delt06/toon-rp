using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing
{
    public class ToonScreenSpaceOutline : ToonPostProcessingPassBase
    {
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int ColorThresholdId = Shader.PropertyToID("_ColorThreshold");
        private static readonly int DepthThresholdId = Shader.PropertyToID("_DepthThreshold");
        private static readonly int NormalsThresholdId = Shader.PropertyToID("_NormalsThreshold");
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

            _material.SetFloat(ColorThresholdId, _settings.ColorThreshold);
            _material.SetKeyword(new LocalKeyword(_shader, "_COLOR"), _settings.UseColor);

            _material.SetFloat(DepthThresholdId, _settings.DepthThreshold);
            _material.SetKeyword(new LocalKeyword(_shader, "_DEPTH"), _settings.UseDepth);

            _material.SetFloat(NormalsThresholdId, _settings.NormalsThreshold);
            _material.SetKeyword(new LocalKeyword(_shader, "_NORMALS"), _settings.UseNormals);

            _material.SetKeyword(new LocalKeyword(_shader, "_USE_FOG"), _settings.UseFog);

            _material.SetVector(DistanceFadeId,
                new Vector4(
                    1.0f / _settings.MaxDistance,
                    1.0f / _settings.DistanceFade
                )
            );
        }
    }
}