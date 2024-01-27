using DELTation.ToonRP.Xr;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    public class ToonSsao : ToonRenderingExtensionBase
    {
        public const int MaxSamplesCount = 64;

        private const GraphicsFormat RtFormat = GraphicsFormat.R8_UNorm;
        private const int MainPass = 0;
        private const int BlurPass = 1;
        public const string ShaderName = "Hidden/Toon RP/SSAO";
        public const string SsaoKeywordName = "_TOON_RP_SSAO";
        public const string SsaoPatternKeywordName = "_TOON_RP_SSAO_PATTERN";
        private static readonly int RtId = Shader.PropertyToID("_ToonRP_SSAOTexture");
        private static readonly int RtTempId = Shader.PropertyToID("_ToonRP_SSAOTexture_Temp");
        private static readonly int RampId = Shader.PropertyToID("_ToonRP_SSAO_Ramp");
        private static readonly int PatternId = Shader.PropertyToID("_ToonRP_SSAO_Pattern");
        private static readonly int PatternScaleId = Shader.PropertyToID("_ToonRP_SSAO_PatternScale");
        private static readonly int NoiseTextureId = Shader.PropertyToID("_ToonRP_SSAO_NoiseTexture");
        private static readonly int RadiusId = Shader.PropertyToID("_ToonRP_SSAO_Radius");
        private static readonly int PowerId = Shader.PropertyToID("_ToonRP_SSAO_Power");
        private static readonly int NoiseScaleId = Shader.PropertyToID("_ToonRP_SSAO_NoiseScale");
        private static readonly int KernelSizeId = Shader.PropertyToID("_ToonRP_SSAO_KernelSize");
        private static readonly int SamplesId = Shader.PropertyToID("_ToonRP_SSAO_Samples");
        private static readonly int BlurDirectionId = Shader.PropertyToID("_ToonRP_SSAO_Blur_Direction");
        private static readonly int BlurSourceId = Shader.PropertyToID("_ToonRP_SSAO_Blur_SourceTex");

        private readonly Vector4[] _samples = GenerateRandomSamples(MaxSamplesCount);
        private readonly GlobalKeyword _ssaoKeyword = GlobalKeyword.Create(SsaoKeywordName);
        private readonly GlobalKeyword _ssaoPatternKeyword = GlobalKeyword.Create(SsaoPatternKeywordName);
        private ToonAdditionalCameraData _additionalCameraData;
        private ScriptableRenderContext _context;
        private int _height;
        private Material _material;
        private Texture _noiseTexture;
        private RenderTargetIdentifier _rtId;
        private RenderTargetIdentifier _rtTempId;
        private ToonSsaoSettings _settings;
        private int _width;

        public override void Setup(in ToonRenderingExtensionContext context,
            IToonRenderingExtensionSettingsStorage settingsStorage)
        {
            _context = context.ScriptableRenderContext;
            _settings = settingsStorage.GetSettings<ToonSsaoSettings>(this);
            _additionalCameraData = context.AdditionalCameraData;

            _width = context.CameraRenderTarget.Width;
            _height = context.CameraRenderTarget.Height;

            _width = Mathf.Max(1, _width / _settings.ResolutionFactor);
            _height = Mathf.Max(1, _height / _settings.ResolutionFactor);
        }

        public override void Render()
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.Ssao)))
            {
                if (_noiseTexture == null)
                {
                    _noiseTexture = GenerateNoiseTexture();
                }

                if (_material == null)
                {
                    var shader = Shader.Find(ShaderName);
                    _material = new Material(shader);
                }

                const FilterMode filterMode = FilterMode.Bilinear;
                var renderTextureDescriptor = new RenderTextureDescriptor(_width, _height, RtFormat, 0);
                _rtId = GetTemporaryRT(cmd, RtId, renderTextureDescriptor, filterMode);
                _rtTempId = GetTemporaryRT(cmd, RtTempId, renderTextureDescriptor, filterMode);

                _context.ExecuteCommandBufferAndClear(cmd);
                ToonXr.BeginXrRendering(ref _context, cmd, _additionalCameraData.XrPass);

                {
                    using (new ProfilingScope(cmd, NamedProfilingSampler.Get("SSAO (Trace)")))
                    {
                        cmd.SetRenderTarget(_rtId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                        RenderMainPass(cmd);
                    }

                    using (new ProfilingScope(cmd, NamedProfilingSampler.Get("SSAO (Blur)")))
                    {
                        cmd.SetRenderTarget(_rtTempId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                        RenderBlur(cmd, Vector2.right, _rtId);
                        cmd.SetRenderTarget(_rtId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                        RenderBlur(cmd, Vector2.up, _rtTempId);
                    }
                }

                _context.ExecuteCommandBufferAndClear(cmd);
                ToonXr.EndXrRendering(ref _context, cmd, _additionalCameraData.XrPass);

                {
                    float effectiveThreshold = 1 - _settings.Threshold;
                    cmd.SetGlobalVector(RampId,
                        ToonRpUtils.BuildRampVectorFromSmoothness(effectiveThreshold, _settings.Smoothness)
                    );
                }

                bool patternEnabled = _settings.Pattern != null;
                cmd.SetKeyword(_ssaoKeyword, !patternEnabled);
                cmd.SetKeyword(_ssaoPatternKeyword, patternEnabled);

                Texture2D patternTexture = _settings.Pattern != null ? _settings.Pattern : Texture2D.blackTexture;
                cmd.SetGlobalTexture(PatternId, patternTexture);
                cmd.SetGlobalVector(PatternScaleId, _settings.PatternScale);
            }

            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        private RenderTargetIdentifier GetTemporaryRT(CommandBuffer cmd,
            int identifier, RenderTextureDescriptor descriptor, FilterMode filterMode)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            XRPass xrPass = _additionalCameraData.XrPass;
            if (xrPass.enabled)
            {
                int arraySize = xrPass.viewCount;
                cmd.GetTemporaryRTArray(identifier, descriptor.width, descriptor.height, arraySize,
                    descriptor.depthBufferBits, filterMode, descriptor.graphicsFormat
                );
                return ToonRpUtils.FixupTextureArrayIdentifier(identifier);
            }
#endif // ENABLE_VR && ENABLE_XR_MODULE

            cmd.GetTemporaryRT(identifier, descriptor, filterMode);
            return identifier;
        }

        public override void Cleanup()
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.ReleaseTemporaryRT(RtId);
            cmd.ReleaseTemporaryRT(RtTempId);
            cmd.DisableKeyword(_ssaoKeyword);
            cmd.DisableKeyword(_ssaoPatternKeyword);
            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);
        }

        private static Texture2D GenerateNoiseTexture()
        {
            const int noiseTextureWidth = 4;
            const int noiseTextureHeight = 4;

            Random.State oldState = Random.state;
            Random.InitState(0);

            var texture = new Texture2D(noiseTextureWidth, noiseTextureHeight, TextureFormat.RG16, false, true)
            {
                name = "SSAO Noise",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point,
            };

            for (int i = 0; i < noiseTextureWidth; i++)
            {
                for (int j = 0; j < noiseTextureHeight; j++)
                {
                    Color color;
                    color.r = Random.value;
                    color.g = Random.value;
                    color.b = color.a = 0;
                    texture.SetPixel(i, j, color);
                }
            }

            Random.state = oldState;

            texture.Apply();

            return texture;
        }

        private static Vector4[] GenerateRandomSamples(int samplesCount)
        {
            Random.State oldState = Random.state;
            Random.InitState(0);

            var samples = new Vector4[samplesCount];

            for (int i = 0; i < samplesCount; ++i)
            {
                var sample = new Vector4(
                    Random.value * 2 - 1,
                    Random.value * 2 - 1,
                    Random.value,
                    0.0f
                );
                sample = Vector4.Normalize(sample);
                sample *= Random.value;

                float scale = i / (float) samplesCount;
                scale = Mathf.LerpUnclamped(0.1f, 1.0f, scale * scale);
                sample *= scale;

                samples[i] = sample;
            }

            Random.state = oldState;

            return samples;
        }


        private void RenderMainPass(CommandBuffer cmd)
        {
            cmd.SetRenderTarget(_rtId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

            cmd.SetGlobalTexture(NoiseTextureId, _noiseTexture);

            cmd.SetGlobalFloat(RadiusId, GetRadius());
            cmd.SetGlobalFloat(PowerId, _settings.Power);

            cmd.SetGlobalVector(NoiseScaleId,
                new Vector4((float) _width / _noiseTexture.width, (float) _height / _noiseTexture.height)
            );
            cmd.SetGlobalInteger(KernelSizeId, _settings.KernelSize);
            cmd.SetGlobalVectorArray(SamplesId, _samples);
            Draw(cmd, MainPass);
        }

        private float GetRadius()
        {
            float radius = _settings.Radius;
            if (ToonGraphicsApi.OpenGlStyleClipDepth)
            {
                radius *= 2;
            }

            return radius;
        }

        private void RenderBlur(CommandBuffer cmd, Vector2 direction, in RenderTargetIdentifier source)
        {
            cmd.SetGlobalVector(BlurDirectionId, direction);
            cmd.SetGlobalTexture(BlurSourceId, source);
            Draw(cmd, BlurPass);
        }

        private void Draw(CommandBuffer cmd, int shaderPass)
        {
            const bool renderToTexture = true;
            ToonBlitter.Blit(cmd, _material, renderToTexture, shaderPass);
        }
    }
}