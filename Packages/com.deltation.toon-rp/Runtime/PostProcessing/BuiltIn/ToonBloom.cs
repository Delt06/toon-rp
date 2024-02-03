using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using static DELTation.ToonRP.ToonRpUtils;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    // https://catlikecoding.com/unity/tutorials/custom-srp/post-processing/
    public class ToonBloom : ToonPostProcessingPassBase
    {
        public const int MaxIterations = 16;
        public const string ShaderName = "Hidden/Toon RP/Bloom";

        private const string DownsampleSampleName = "Downsample";
        private const string CombineSampleName = "Combine";
        private const string PrefilterSampleName = "Prefilter";

        private const int HorizontalBlurPass = 0;
        private const int VerticalBlurPass = 1;
        private const int CombinePass = 2;
        private const int PrefilterPass = 3;
        private const float MinPatternSize = 3;

        private static readonly int MainTex2Id = Shader.PropertyToID("_MainTex2");
        private static readonly int PrefilterSourceId = Shader.PropertyToID("_ToonRP_Bloom_Prefilter");
        private static readonly int ThresholdId = Shader.PropertyToID("_ToonRP_Bloom_Threshold");
        private static readonly int IntensityId = Shader.PropertyToID("_ToonRP_Bloom_Intensity");
        private static readonly int UsePatternId = Shader.PropertyToID("_ToonRP_Bloom_UsePattern");
        private static readonly int PatternScaleId = Shader.PropertyToID("_ToonRP_Bloom_PatternScale");
        private static readonly int PatternPowerId = Shader.PropertyToID("_ToonRP_Bloom_PatternPower");
        private static readonly int PatternMultiplierId = Shader.PropertyToID("_ToonRP_Bloom_PatternMultiplier");
        private static readonly int PatternEdgeId = Shader.PropertyToID("_ToonRP_Bloom_PatternEdge");
        private static readonly int PatternLuminanceThresholdId =
            Shader.PropertyToID("_ToonRP_Bloom_PatternLuminanceThreshold");
        private static readonly int PatternDotSizeLimitId = Shader.PropertyToID("_ToonRP_Bloom_PatternDotSizeLimit");
        private static readonly int PatternBlendId =
            Shader.PropertyToID("_ToonRP_Bloom_PatternBlend");
        private static readonly int PatternFinalIntensityRampId =
            Shader.PropertyToID("_ToonRP_Bloom_PatternFinalIntensityRamp");

        private readonly int _bloomPyramidId;

        private readonly ToonPipelineMaterial _material = new(ShaderName, "Toon RP Bloom");
        private ToonCameraRendererSettings _cameraRendererSettings;
        private ToonPostProcessingContext _postProcessingContext;
        private ToonBloomSettings _settings;

        public ToonBloom()
        {
            _bloomPyramidId = Shader.PropertyToID("_ToonRP_BloomPyramid0");

            for (int i = 1; i < MaxIterations; i++)
            {
                Shader.PropertyToID("_ToonRP_BloomPyramid" + i);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _material.Dispose();
        }

        public override void Setup(CommandBuffer cmd, in ToonPostProcessingContext context)
        {
            base.Setup(cmd, in context);
            _settings = context.Settings.Find<ToonBloomSettings>();
            _postProcessingContext = context;
        }

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination, bool destinationIsIntermediateTexture)
        {
            int rtWidth = Context.RtWidth;
            int rtHeight = Context.RtHeight;
            int resolutionFactor = _settings.ResolutionFactor;

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.Bloom)))
            {
                int width = rtWidth / resolutionFactor, height = rtHeight / resolutionFactor;
                int downscaleLimit = _settings.DownsampleLimit;

                if (_settings.MaxIterations == 0 || _settings.Intensity <= 0.0f ||
                    height < downscaleLimit || width < downscaleLimit)
                {
                    BlitDefault(cmd, source, destination);
                    return;
                }

                Material material = _material.GetOrCreate();
                Prefilter(cmd, material, source, width, height);

                width /= 2;
                height /= 2;

                int fromId = PrefilterSourceId, toId = _bloomPyramidId + 1;

                int i = Downsample(cmd, material, height, width, ref toId, ref fromId);
                Combine(cmd, material, source, destination, destinationIsIntermediateTexture, i, fromId, toId);

                cmd.ReleaseTemporaryRT(PrefilterSourceId);
            }
        }

        private void Prefilter(CommandBuffer cmd, Material material, RenderTargetIdentifier source, int width,
            int height)
        {
            cmd.BeginSample(PrefilterSampleName);
            GetTemporaryRT(cmd, PrefilterSourceId, width, height, FilterMode.Bilinear, Context.ColorFormat);

            {
                Vector4 threshold;
                threshold.x = Mathf.GammaToLinearSpace(_settings.Threshold);
                threshold.y = threshold.x * _settings.ThresholdKnee;
                threshold.z = 2.0f * threshold.y;
                threshold.w = 0.25f / (threshold.y + 0.00001f);
                threshold.y -= threshold.x;
                cmd.SetGlobalVector(ThresholdId, threshold);
            }

            Blit(cmd, source, PrefilterSourceId, material, PrefilterPass);
            cmd.EndSample(PrefilterSampleName);
        }

        private int Downsample(CommandBuffer cmd, Material material, int height, int width, ref int toId,
            ref int fromId)
        {
            cmd.BeginSample(DownsampleSampleName);
            int i;
            for (i = 0; i < _settings.MaxIterations; ++i)
            {
                if (height < _settings.DownsampleLimit || width < _settings.DownsampleLimit)
                {
                    break;
                }

                int midId = toId - 1;
                GetTemporaryRT(cmd, midId, width, height, FilterMode.Bilinear, Context.ColorFormat);
                GetTemporaryRT(cmd, toId, width, height, FilterMode.Bilinear, Context.ColorFormat);

                Blit(cmd, fromId, midId, material, HorizontalBlurPass);
                Blit(cmd, midId, toId, material, VerticalBlurPass);

                fromId = toId;
                toId += 2;
                width /= 2;
                height /= 2;
            }

            cmd.EndSample(DownsampleSampleName);
            return i;
        }

        private static void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination,
            Material material, int shaderPass, bool renderToTexture = true)
        {
            cmd.SetRenderTarget(FixupTextureArrayIdentifier(destination), RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store
            );
            cmd.SetGlobalTexture(ToonBlitter.MainTexId, FixupTextureArrayIdentifier(source));
            ToonBlitter.Blit(cmd, material, renderToTexture, shaderPass);
        }

        private static void BlitDefault(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination)
        {
            cmd.SetRenderTarget(FixupTextureArrayIdentifier(destination), RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store
            );
            ToonBlitter.BlitDefault(cmd, source, true);
        }

        private void Combine(CommandBuffer cmd, Material material,
            RenderTargetIdentifier source, RenderTargetIdentifier destination, bool destinationIsIntermediateTexture,
            int i, int fromId,
            int toId)
        {
            cmd.BeginSample(CombineSampleName);
            cmd.SetGlobalFloat(IntensityId, 1.0f);
            cmd.SetGlobalInteger(UsePatternId, 0);
            if (i > 1)
            {
                cmd.ReleaseTemporaryRT(fromId - 1);
                toId -= 5;

                i--;

                for (; i > 0; i--)
                {
                    cmd.SetGlobalTexture(MainTex2Id, FixupTextureArrayIdentifier(toId + 1));
                    Blit(cmd, fromId, toId, material, CombinePass);
                    cmd.ReleaseTemporaryRT(fromId);
                    cmd.ReleaseTemporaryRT(toId + 1);
                    fromId = toId;
                    toId -= 2;
                }
            }
            else
            {
                cmd.ReleaseTemporaryRT(_bloomPyramidId);
            }

            cmd.SetGlobalFloat(IntensityId, _settings.Intensity);

            if (_settings.Pattern.Enabled)
            {
                cmd.SetGlobalInteger(UsePatternId, 1);

                {
                    float minPatternScale = Mathf.Min(Context.RtWidth, Context.RtHeight) / MinPatternSize;
                    float patternScale = Mathf.Min(_settings.Pattern.Scale, minPatternScale);
                    cmd.SetGlobalFloat(PatternScaleId, patternScale);
                }

                cmd.SetGlobalFloat(PatternPowerId, _settings.Pattern.Power);
                cmd.SetGlobalFloat(PatternMultiplierId, _settings.Pattern.Multiplier);
                cmd.SetGlobalFloat(PatternEdgeId, 1 - _settings.Pattern.Smoothness);
                cmd.SetGlobalFloat(PatternLuminanceThresholdId, _settings.Pattern.LuminanceThreshold);
                cmd.SetGlobalFloat(PatternDotSizeLimitId,
                    _settings.Pattern.DotSizeLimit > 0.0f ? _settings.Pattern.DotSizeLimit : 10.0f
                );
                cmd.SetGlobalFloat(PatternBlendId, _settings.Pattern.Blend);
                cmd.SetGlobalVector(PatternFinalIntensityRampId,
                    BuildRampVectorFromEdges(_settings.Pattern.FinalIntensityThreshold, 1.0f)
                );
            }

            cmd.SetGlobalTexture(MainTex2Id, FixupTextureArrayIdentifier(source));
            Blit(cmd, fromId, destination, material, CombinePass, destinationIsIntermediateTexture);
            cmd.ReleaseTemporaryRT(fromId);

            cmd.EndSample(CombineSampleName);
        }

        private void GetTemporaryRT(CommandBuffer cmd, int id, int width, int height,
            FilterMode filterMode, GraphicsFormat format)
        {
            const int depthBuffer = 0;

#if ENABLE_VR && ENABLE_XR_MODULE
            XRPass xrPass = _postProcessingContext.AdditionalCameraData.XrPass;
            if (xrPass.enabled)
            {
                int arraySize = xrPass.viewCount;
                cmd.GetTemporaryRTArray(id, width, height, arraySize, depthBuffer, filterMode, format);
            }
            else
#endif // ENABLE_VR && ENABLE_XR_MODULE
            {
                cmd.GetTemporaryRT(id, width, height, depthBuffer, filterMode, format);
            }
        }
    }
}