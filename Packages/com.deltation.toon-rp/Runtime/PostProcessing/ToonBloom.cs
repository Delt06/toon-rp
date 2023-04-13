using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing
{
    // https://catlikecoding.com/unity/tutorials/custom-srp/post-processing/
    public class ToonBloom
    {
        public const int MaxIterations = 16;

        private const string BaseSampleName = "Bloom";
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

        private RenderTextureFormat _colorFormat;

        private Material _material;
        private int _rtHeight;
        private int _rtWidth;
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

            _material = new Material(Shader.Find("Hidden/Toon RP/Bloom"))
            {
                name = "Toon RP Bloom",
            };
        }

        public void Setup(in ToonBloomSettings settings, RenderTextureFormat colorFormat, int rtWidth, int rtHeight)
        {
            _settings = settings;
            _colorFormat = colorFormat;
            _rtWidth = rtWidth;
            _rtHeight = rtHeight;
        }

        public void Render(CommandBuffer cmd, int sourceId, RenderTargetIdentifier destination)
        {
            EnsureMaterialIsCreated();

            cmd.BeginSample(BaseSampleName);

            int width = _rtWidth / 2, height = _rtHeight / 2;

            int downscaleLimit = _settings.DownsampleLimit * 2;

            if (_settings.MaxIterations == 0 || _settings.Intensity <= 0.0f ||
                height < downscaleLimit || width < downscaleLimit)
            {
                cmd.Blit(sourceId, destination);
                cmd.EndSample(BaseSampleName);
                return;
            }


            Prefilter(cmd, sourceId, width, height);

            width /= 2;
            height /= 2;

            int fromId = PrefilterSourceId, toId = _bloomPyramidId + 1;

            int i = Downsample(cmd, height, width, ref toId, ref fromId);
            Combine(cmd, sourceId, destination, i, fromId, toId);

            cmd.ReleaseTemporaryRT(PrefilterSourceId);
            cmd.EndSample(BaseSampleName);
        }

        private void Prefilter(CommandBuffer cmd, int sourceId, int width, int height)
        {
            cmd.BeginSample(PrefilterSampleName);
            cmd.GetTemporaryRT(PrefilterSourceId, width, height, 0, FilterMode.Bilinear, _colorFormat);

            {
                Vector4 threshold;
                threshold.x = Mathf.GammaToLinearSpace(_settings.Threshold);
                threshold.y = threshold.x * _settings.ThresholdKnee;
                threshold.z = 2.0f * threshold.y;
                threshold.w = 0.25f / (threshold.y + 0.00001f);
                threshold.y -= threshold.x;
                cmd.SetGlobalVector(ThresholdId, threshold);
            }

            cmd.Blit(sourceId, PrefilterSourceId, _material, PrefilterPass);
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
                cmd.GetTemporaryRT(midId, width, height, 0, FilterMode.Point, _colorFormat);
                cmd.GetTemporaryRT(toId, width, height, 0, FilterMode.Point, _colorFormat);

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

        private void Combine(CommandBuffer cmd, int sourceId, RenderTargetIdentifier destination, int i, int fromId,
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
                    float minPatternScale = Mathf.Min(_rtWidth, _rtHeight) / MinPatternSize;
                    float patternScale = Mathf.Min(_settings.Pattern.Scale, minPatternScale);
                    cmd.SetGlobalFloat(PatternScaleId, patternScale);
                }

                cmd.SetGlobalFloat(PatternPowerId, _settings.Pattern.Power);
                cmd.SetGlobalFloat(PatternMultiplierId, _settings.Pattern.Multiplier);
                cmd.SetGlobalFloat(PatternEdgeId, 1 - _settings.Pattern.Smoothness);
            }

            cmd.SetGlobalTexture(MainTex2Id, sourceId);
            cmd.Blit(fromId, destination, _material, CombinePass);
            cmd.ReleaseTemporaryRT(fromId);

            cmd.EndSample(CombineSampleName);
        }
    }
}