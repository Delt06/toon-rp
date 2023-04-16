using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing
{
    public class ToonSsao
    {
        public const int MaxSamplesCount = 64;

        private const GraphicsFormat RtFormat = GraphicsFormat.R8_UNorm;
        private const int MainPass = 0;
        private const int BlurPass = 1;
        private static readonly int RtId = Shader.PropertyToID("_ToonRP_SSAOTexture");
        private static readonly int RtTempId = Shader.PropertyToID("_ToonRP_SSAOTexture_Temp");
        private static readonly int RampId = Shader.PropertyToID("_ToonRP_SSAO_Ramp");
        private static readonly int PatternScaleId = Shader.PropertyToID("_ToonRP_SSAO_Pattern_Scale");
        private static readonly int PatternRampId = Shader.PropertyToID("_ToonRP_SSAO_Pattern_Ramp");
        private static readonly int PatternDistanceFade = Shader.PropertyToID("_ToonRP_SSAO_Pattern_DistanceFade");
        private static readonly int NoiseTextureId = Shader.PropertyToID("_ToonRP_SSAO_NoiseTexture");
        private static readonly int RadiusId = Shader.PropertyToID("_ToonRP_SSAO_Radius");
        private static readonly int PowerId = Shader.PropertyToID("_ToonRP_SSAO_Power");
        private static readonly int NoiseScaleId = Shader.PropertyToID("_ToonRP_SSAO_NoiseScale");
        private static readonly int KernelSizeId = Shader.PropertyToID("_ToonRP_SSAO_KernelSize");
        private static readonly int SamplesId = Shader.PropertyToID("_ToonRP_SSAO_Samples");
        private static readonly int BlurDirectionId = Shader.PropertyToID("_ToonRP_SSAO_Blur_Direction");
        private static readonly int BlurSourceId = Shader.PropertyToID("_ToonRP_SSAO_Blur_SourceTex");

        private readonly Vector4[] _samples;
        private readonly GlobalKeyword _ssaoKeyword;
        private readonly GlobalKeyword _ssaoPatternKeyword;
        private ScriptableRenderContext _context;
        private int _height;
        private Material _material;
        private Texture _noiseTexture;
        private ToonSsaoSettings _settings;
        private int _width;

        public ToonSsao()
        {
            _samples = GenerateRandomSamples(MaxSamplesCount);
            _ssaoKeyword = GlobalKeyword.Create("_TOON_RP_SSAO");
            _ssaoPatternKeyword = GlobalKeyword.Create("_TOON_RP_SSAO_PATTERN");
        }

        private static Texture2D GenerateNoiseTexture()
        {
            const int noiseTextureWidth = 4;
            const int noiseTextureHeight = 4;

            Random.State oldState = Random.state;
            Random.InitState(0);

            var texture = new Texture2D(noiseTextureWidth, noiseTextureHeight, GraphicsFormat.R32G32_SFloat, 0,
                TextureCreationFlags.None
            )
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
                    color.r = Random.value * 2 - 1;
                    color.g = Random.value * 2 - 1;
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

        public void Setup(in ScriptableRenderContext context,
            in ToonSsaoSettings toonSsaoSettings, int rtWidth, int rtHeight)
        {
            _context = context;
            _settings = toonSsaoSettings;
            _height = rtHeight;
            _width = rtWidth;

            if (_settings.HalfResolution)
            {
                _width /= 2;
                _height /= 2;
            }

            CommandBuffer cmd = CommandBufferPool.Get();
            bool patternEnabled = _settings.Pattern.Enabled;
            cmd.SetKeyword(_ssaoKeyword, !patternEnabled);
            cmd.SetKeyword(_ssaoPatternKeyword, patternEnabled);
            ExecuteBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Render()
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
                    var shader = Shader.Find("Hidden/Toon RP/SSAO");
                    _material = new Material(shader);
                }

                const FilterMode filterMode = FilterMode.Bilinear;
                cmd.GetTemporaryRT(RtId, _width, _height, 0, filterMode, RtFormat);
                cmd.GetTemporaryRT(RtTempId, _width, _height, 0, filterMode, RtFormat);

                {
                    const string sampleName = "SSAO (Trace)";
                    cmd.BeginSample(sampleName);
                    cmd.SetRenderTarget(RtId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                    RenderMainPass(cmd);
                    cmd.EndSample(sampleName);
                }

                {
                    const string sampleName = "SSAO (Blur)";
                    cmd.BeginSample(sampleName);
                    cmd.SetRenderTarget(RtTempId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                    RenderBlur(cmd, Vector2.right, RtId);
                    cmd.SetRenderTarget(RtId);
                    RenderBlur(cmd, Vector2.up, RtTempId);
                    cmd.EndSample(sampleName);
                }

                {
                    float effectiveThreshold = 1 - _settings.Threshold;
                    cmd.SetGlobalVector(RampId,
                        new Vector4(effectiveThreshold, effectiveThreshold + _settings.Smoothness)
                    );
                }

                if (_settings.Pattern.Enabled)
                {
                    cmd.SetGlobalVector(PatternScaleId, _settings.Pattern.Scale);
                    float threshold = _settings.Pattern.Thickness;
                    cmd.SetGlobalVector(PatternRampId,
                        new Vector4(threshold, threshold + _settings.Pattern.Smoothness)
                    );
                    cmd.SetGlobalVector(PatternDistanceFade, new Vector4(
                            1.0f / _settings.Pattern.MaxDistance,
                            1.0f / _settings.Pattern.DistanceFade
                        )
                    );
                }
            }

            ExecuteBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }


        private void RenderMainPass(CommandBuffer cmd)
        {
            cmd.SetRenderTarget(RtId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

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
            if (GraphicsApiUtils.OpenGlStyleClipDepth)
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
            cmd.DrawProcedural(Matrix4x4.identity, _material, shaderPass, MeshTopology.Triangles, 3, 1);
        }

        public void Cleanup()
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.ReleaseTemporaryRT(RtId);
            cmd.ReleaseTemporaryRT(RtTempId);
            ExecuteBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void ExecuteBuffer(CommandBuffer cmd)
        {
            _context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        public void SetupDisabled(in ScriptableRenderContext context)
        {
            _context = context;
            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.DisableKeyword(_ssaoKeyword);
            cmd.DisableKeyword(_ssaoPatternKeyword);
            ExecuteBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}