using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    public class ToonLightScattering : ToonPostProcessingPassBase
    {
        private const int ComputePass = 0;
        private const int CombinePass = 1;
        public const string ShaderName = "Hidden/Toon RP/Light Scattering";

        private static readonly int ScatteringTextureId = Shader.PropertyToID("_ToonRP_ScatteringTexture");
        private static readonly int CenterId = Shader.PropertyToID("_Center");
        private static readonly int ThresholdId = Shader.PropertyToID("_Threshold");
        private static readonly int BlurWidthId = Shader.PropertyToID("_BlurWidth");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int NumSamplesId = Shader.PropertyToID("_NumSamples");
        private ToonLightScatteringSettings _lightScatteringSettings;
        private Material _material;

        private void EnsureMaterialIsCreated()
        {
            if (_material != null)
            {
                return;
            }

            _material = new Material(Shader.Find(ShaderName))
            {
                name = "Toon RP Light Scattering",
            };
        }

        public override bool NeedsDistinctSourceAndDestination() => false;

        public override void Setup(CommandBuffer cmd, in ToonPostProcessingContext context)
        {
            base.Setup(cmd, in context);
            _lightScatteringSettings = context.Settings.Find<ToonLightScatteringSettings>();
        }

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination)
        {
            EnsureMaterialIsCreated();

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.LightScattering)))
            {
                Light sun = RenderSettings.sun;
                Camera camera = Context.Camera;
                Vector3 sunPosition = camera.transform.position - sun.transform.forward * 1000;
                Vector2 sunViewport = camera.WorldToViewportPoint(sunPosition);

                _material.SetVector(CenterId, sunViewport);
                _material.SetFloat(ThresholdId, _lightScatteringSettings.Threshold);
                _material.SetFloat(BlurWidthId, _lightScatteringSettings.BlurWidth);
                _material.SetFloat(IntensityId, _lightScatteringSettings.Intensity);
                _material.SetInteger(NumSamplesId, _lightScatteringSettings.Samples);

                int resolutionFactor = Mathf.Max(1, _lightScatteringSettings.ResolutionFactor);
                int scatteringWidth = Mathf.Max(1, Context.RtWidth / resolutionFactor);
                int scatteringHeight = Mathf.Max(1, Context.RtHeight / resolutionFactor);
                var descriptor = new RenderTextureDescriptor(
                    scatteringWidth, scatteringHeight,
                    Context.ColorFormat, 0, 0
                );
                cmd.GetTemporaryRT(ScatteringTextureId, descriptor, FilterMode.Bilinear);

                bool useScissor = _lightScatteringSettings.ScissorRadius > 0.0f;

                using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Compute")))
                {
                    if (useScissor)
                    {
                        cmd.EnableScissorRect(ComputeScissorRect(sunViewport, scatteringWidth, scatteringHeight));
                    }

                    cmd.Blit(source, ScatteringTextureId, _material, ComputePass);
                }

                using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Combine")))
                {
                    if (useScissor)
                    {
                        cmd.EnableScissorRect(ComputeScissorRect(sunViewport, Context.RtWidth, Context.RtHeight));
                    }

                    cmd.Blit(source, destination, _material, CombinePass);
                }

                if (useScissor)
                {
                    cmd.DisableScissorRect();
                }

                cmd.ReleaseTemporaryRT(ScatteringTextureId);
            }
        }

        private Rect ComputeScissorRect(Vector2 sunViewport, int rtWidth, int rtHeight)
        {
            int maxSide = Mathf.Max(rtWidth, rtHeight);
            Vector2 halfSize = new Vector2(maxSide, maxSide) * _lightScatteringSettings.ScissorRadius;
            Vector2 center = sunViewport * new Vector2(rtWidth, rtHeight);
            Vector2 min = center - halfSize;
            return new Rect(min, halfSize * 2);
        }
    }
}