using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace ToonRP.Runtime.PostProcessing
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

        private readonly CommandBuffer _cmd = new()
        {
            name = "SSAO",
        };
        private readonly Vector4[] _samples;
        private readonly GlobalKeyword _ssaoKeyword;
        private readonly GlobalKeyword _ssaoPatternKeyword;
        private ScriptableRenderContext _context;
        private int _height;
        private Texture _noiseTexture;
        private ToonSsaoSettings _settings;
        private Material _traceMaterial;
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

            bool patternEnabled = _settings.Pattern.Enabled;
            _cmd.SetKeyword(_ssaoKeyword, !patternEnabled);
            _cmd.SetKeyword(_ssaoPatternKeyword, patternEnabled);
            ExecuteBuffer();
        }

        public void Render()
        {
            if (_noiseTexture == null)
            {
                _noiseTexture = GenerateNoiseTexture();
            }

            if (_traceMaterial == null)
            {
                var shader = Shader.Find("Hidden/Toon RP/SSAO");
                _traceMaterial = new Material(shader);
            }

            const FilterMode filterMode = FilterMode.Bilinear;
            _cmd.GetTemporaryRT(RtId, _width, _height, 0, filterMode, RtFormat);
            _cmd.GetTemporaryRT(RtTempId, _width, _height, 0, filterMode, RtFormat);

            {
                const string sampleName = "SSAO";
                _cmd.BeginSample(sampleName);
                _cmd.SetRenderTarget(RtId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                RenderMainPass();
                _cmd.EndSample(sampleName);
            }

            {
                const string sampleName = "Blur";
                _cmd.BeginSample(sampleName);
                _cmd.SetRenderTarget(RtTempId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                RenderBlur(Vector2.right, RtId);
                _cmd.SetRenderTarget(RtId);
                RenderBlur(Vector2.up, RtTempId);
                _cmd.EndSample(sampleName);
            }

            {
                float effectiveThreshold = 1 - _settings.Threshold;
                _cmd.SetGlobalVector(RampId,
                    new Vector4(effectiveThreshold, effectiveThreshold + _settings.Smoothness)
                );
            }

            if (_settings.Pattern.Enabled)
            {
                _cmd.SetGlobalVector(PatternScaleId, _settings.Pattern.Scale);
                float threshold = _settings.Pattern.Thickness;
                _cmd.SetGlobalVector(PatternRampId,
                    new Vector4(threshold, threshold + _settings.Pattern.Smoothness)
                );
                _cmd.SetGlobalVector(PatternDistanceFade, new Vector4(
                        1.0f / _settings.Pattern.MaxDistance,
                        1.0f / _settings.Pattern.DistanceFade
                    )
                );
            }

            ExecuteBuffer();
        }


        private void RenderMainPass()
        {
            _cmd.SetRenderTarget(RtId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

            _cmd.SetGlobalTexture(NoiseTextureId, _noiseTexture);

            _cmd.SetGlobalFloat(RadiusId, GetRadius());
            _cmd.SetGlobalFloat(PowerId, _settings.Power);
            _cmd.SetGlobalVector(NoiseScaleId,
                new Vector4((float) _width / _noiseTexture.width, (float) _height / _noiseTexture.height)
            );
            _cmd.SetGlobalInteger(KernelSizeId, _settings.KernelSize);
            _cmd.SetGlobalVectorArray(SamplesId, _samples);
            Draw(MainPass);
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

        private void RenderBlur(Vector2 direction, in RenderTargetIdentifier source)
        {
            _cmd.SetGlobalVector(BlurDirectionId, direction);
            _cmd.SetGlobalTexture(BlurSourceId, source);
            Draw(BlurPass);
        }

        private void Draw(int shaderPass)
        {
            _cmd.DrawProcedural(Matrix4x4.identity, _traceMaterial, shaderPass, MeshTopology.Triangles, 3, 1);
        }

        public void Cleanup()
        {
            _cmd.ReleaseTemporaryRT(RtId);
            _cmd.ReleaseTemporaryRT(RtTempId);
            ExecuteBuffer();
        }

        private void ExecuteBuffer()
        {
            _context.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }

        public void SetupDisabled(in ScriptableRenderContext context)
        {
            _context = context;
            _cmd.DisableKeyword(_ssaoKeyword);
            _cmd.DisableKeyword(_ssaoPatternKeyword);
            ExecuteBuffer();
        }
    }
}