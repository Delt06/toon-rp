using UnityEngine;
using UnityEngine.Rendering;

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
        private static readonly int UseBicubicUpsamplingId = Shader.PropertyToID("_ToonRP_Bloom_UseBicubicUpsampling");
        private static readonly int PrefilterSourceId = Shader.PropertyToID("_ToonRP_Bloom_Prefilter");
        private static readonly int ThresholdId = Shader.PropertyToID("_ToonRP_Bloom_Threshold");
        private static readonly int IntensityId = Shader.PropertyToID("_ToonRP_Bloom_Intensity");
        private static readonly int UsePatternId = Shader.PropertyToID("_ToonRP_Bloom_UsePattern");
        private static readonly int PatternScaleId = Shader.PropertyToID("_ToonRP_Bloom_PatternScale");
        private static readonly int PatternPowerId = Shader.PropertyToID("_ToonRP_Bloom_PatternPower");
        private static readonly int PatternMultiplierId = Shader.PropertyToID("_ToonRP_Bloom_PatternMultiplier");
        private static readonly int PatternEdgeId = Shader.PropertyToID("_ToonRP_Bloom_PatternEdge");

        private readonly int _bloomPyramidId;
        private ToonCameraRendererSettings _cameraRendererSettings;

        private Material _material;
        private ToonBloomSettings _settings;

        public ToonBloom()
        {
            _bloomPyramidId = Shader.PropertyToID("_ToonRP_BloomPyramid0");

            for (int i = 1; i < MaxIterations; i++)
            {
                Shader.PropertyToID("_ToonRP_BloomPyramid" + i);
            }
        }

        private void EnsureMaterialIsCreated()
        {
            if (_material != null)
            {
                return;
            }

            _material = new Material(Shader.Find(ShaderName))
            {
                name = "Toon RP Bloom",
            };
        }

        public override void Setup(CommandBuffer cmd, in ToonPostProcessingContext context)
        {
            base.Setup(cmd, in context);
            _settings = context.Settings.Find<ToonBloomSettings>();
        }

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination)
        {
            EnsureMaterialIsCreated();

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
                    cmd.Blit(source, destination);
                    return;
                }


                Prefilter(cmd, source, width, height);

                width /= 2;
                height /= 2;

                int fromId = PrefilterSourceId, toId = _bloomPyramidId + 1;

                int i = Downsample(cmd, height, width, ref toId, ref fromId);
                Combine(cmd, source, destination, i, fromId, toId);

                cmd.ReleaseTemporaryRT(PrefilterSourceId);
            }
        }

        private void Prefilter(CommandBuffer cmd, RenderTargetIdentifier source, int width, int height)
        {
            cmd.BeginSample(PrefilterSampleName);
            cmd.GetTemporaryRT(PrefilterSourceId, width, height, 0, FilterMode.Bilinear, Context.ColorFormat);

            {
                Vector4 threshold;
                threshold.x = Mathf.GammaToLinearSpace(_settings.Threshold);
                threshold.y = threshold.x * _settings.ThresholdKnee;
                threshold.z = 2.0f * threshold.y;
                threshold.w = 0.25f / (threshold.y + 0.00001f);
                threshold.y -= threshold.x;
                cmd.SetGlobalVector(ThresholdId, threshold);
            }

            cmd.Blit(source, PrefilterSourceId, _material, PrefilterPass);
            cmd.EndSample(PrefilterSampleName);
        }

        private int Downsample(CommandBuffer cmd, int height, int width, ref int toId, ref int fromId)
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
                cmd.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, Context.ColorFormat);
                cmd.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, Context.ColorFormat);

                cmd.Blit(fromId, midId, _material, HorizontalBlurPass);
                cmd.Blit(midId, toId, _material, VerticalBlurPass);

                fromId = toId;
                toId += 2;
                width /= 2;
                height /= 2;
            }

            cmd.EndSample(DownsampleSampleName);
            return i;
        }

        private void Combine(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination,
            int i, int fromId,
            int toId)
        {
            cmd.BeginSample(CombineSampleName);
            cmd.SetGlobalInteger(UseBicubicUpsamplingId, _settings.BicubicUpsampling ? 1 : 0);
            cmd.SetGlobalFloat(IntensityId, 1.0f);
            cmd.SetGlobalInteger(UsePatternId, 0);
            if (i > 1)
            {
                cmd.ReleaseTemporaryRT(fromId - 1);
                toId -= 5;

                i--;

                for (; i > 0; i--)
                {
                    cmd.SetGlobalTexture(MainTex2Id, toId + 1);
                    cmd.Blit(fromId, toId, _material, CombinePass);
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
            }

            cmd.SetGlobalTexture(MainTex2Id, source);
            cmd.Blit(fromId, destination, _material, CombinePass);
            cmd.ReleaseTemporaryRT(fromId);

            cmd.EndSample(CombineSampleName);
        }
    }
}