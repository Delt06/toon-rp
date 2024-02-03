using UnityEngine;
using UnityEngine.Experimental.Rendering;
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
        private readonly ToonPipelineMaterial _material = new(ShaderName, "Toon RP Light Scattering");
        private ToonAdditionalCameraData _additionalCameraData;
        private ToonLightScatteringSettings _lightScatteringSettings;

        public override void Dispose()
        {
            base.Dispose();
            _material.Dispose();
        }

        public override bool NeedsDistinctSourceAndDestination() => false;

        public override void Setup(CommandBuffer cmd, in ToonPostProcessingContext context)
        {
            base.Setup(cmd, in context);
            _lightScatteringSettings = context.Settings.Find<ToonLightScatteringSettings>();
            _additionalCameraData = context.AdditionalCameraData;
        }

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination, bool destinationIsIntermediateTexture)
        {
            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.LightScattering)))
            {
                Light sun = RenderSettings.sun;
                Camera camera = Context.Camera;
                Vector3 sunPosition = camera.transform.position - sun.transform.forward * 1000;
                Vector2 sunViewport = camera.WorldToViewportPoint(sunPosition);

                Material material = _material.GetOrCreate();
                material.SetVector(CenterId, sunViewport);
                material.SetFloat(ThresholdId, _lightScatteringSettings.Threshold);
                material.SetFloat(BlurWidthId, _lightScatteringSettings.BlurWidth);
                material.SetFloat(IntensityId, _lightScatteringSettings.Intensity);
                material.SetInteger(NumSamplesId, _lightScatteringSettings.Samples);

                int resolutionFactor = Mathf.Max(1, _lightScatteringSettings.ResolutionFactor);
                int scatteringWidth = Mathf.Max(1, Context.RtWidth / resolutionFactor);
                int scatteringHeight = Mathf.Max(1, Context.RtHeight / resolutionFactor);
                var descriptor = new RenderTextureDescriptor(
                    scatteringWidth, scatteringHeight,
                    Context.ColorFormat, 0, 0
                );
                RenderTargetIdentifier scatteringTextureId =
                    EnsureTemporaryRT(cmd, ScatteringTextureId, descriptor, FilterMode.Bilinear);

                bool useScissor = _lightScatteringSettings.ScissorRadius > 0.0f;

                using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Compute")))
                {
                    if (useScissor)
                    {
                        cmd.EnableScissorRect(ComputeScissorRect(sunViewport, scatteringWidth, scatteringHeight));
                    }

                    cmd.SetRenderTarget(scatteringTextureId, RenderBufferLoadAction.DontCare,
                        RenderBufferStoreAction.Store
                    );
                    cmd.SetGlobalTexture(ToonBlitter.MainTexId, source);
                    const bool renderToTexture = true;
                    ToonBlitter.Blit(cmd, material, renderToTexture, ComputePass);
                }

                using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Combine")))
                {
                    if (useScissor)
                    {
                        cmd.EnableScissorRect(ComputeScissorRect(sunViewport, Context.RtWidth, Context.RtHeight));
                    }

                    cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                    cmd.SetGlobalTexture(ToonBlitter.MainTexId, source);
                    ToonBlitter.Blit(cmd, material, destinationIsIntermediateTexture, CombinePass);
                }

                if (useScissor)
                {
                    cmd.DisableScissorRect();
                }

                cmd.ReleaseTemporaryRT(ScatteringTextureId);
            }
        }

        private RenderTargetIdentifier EnsureTemporaryRT(CommandBuffer cmd, int id, RenderTextureDescriptor descriptor,
            FilterMode filterMode)
        {
            int arraySize = 1;

#if ENABLE_VR && ENABLE_XR_MODULE
            {
                XRPass xrPass = _additionalCameraData.XrPass;
                if (xrPass.enabled)
                {
                    arraySize = xrPass.viewCount;
                }
            }
#endif // ENABLE_VR && ENABLE_XR_MODULE

            if (arraySize > 1)
            {
                cmd.GetTemporaryRTArray(id, descriptor.width, descriptor.height, arraySize,
                    descriptor.depthBufferBits, filterMode, descriptor.graphicsFormat, descriptor.msaaSamples
                );
                return ToonRpUtils.FixupTextureArrayIdentifier(id);
            }

            cmd.GetTemporaryRT(id, descriptor, filterMode);
            return id;
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