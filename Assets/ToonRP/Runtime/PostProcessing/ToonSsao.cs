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

        private readonly CommandBuffer _cmd = new()
        {
            name = "SSAO",
        };
        private readonly Vector4[] _samples;
        private readonly GlobalKeyword _ssaoKeyword;
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
        }

        private static Texture2D GenerateNoiseTexture()
        {
            const int noiseTextureWidth = 4;
            const int noiseTextureHeight = 4;

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

            texture.Apply();

            return texture;
        }

        private static Vector4[] GenerateRandomSamples(int samplesCount)
        {
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

            _cmd.EnableKeyword(_ssaoKeyword);
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

            float effectiveThreshold = 1 - _settings.Threshold;
            _cmd.SetGlobalVector("_ToonRP_SSAO_Ramp",
                new Vector4(effectiveThreshold, effectiveThreshold + _settings.Smoothness)
            );

            ExecuteBuffer();
        }


        private void RenderMainPass()
        {
            _cmd.SetRenderTarget(RtId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

            _cmd.SetGlobalTexture("_NoiseTexture", _noiseTexture);
            _cmd.SetGlobalFloat("_Radius", _settings.Radius);
            _cmd.SetGlobalFloat("_Power", _settings.Power);
            _cmd.SetGlobalVector("_NoiseScale",
                new Vector4((float) _width / _noiseTexture.width, (float) _height / _noiseTexture.height)
            );
            _cmd.SetGlobalInteger("_KernelSize", _settings.KernelSize);
            _cmd.SetGlobalVectorArray("_Samples", _samples);
            Draw(MainPass);
        }

        private void RenderBlur(Vector2 direction, in RenderTargetIdentifier source)
        {
            _cmd.SetGlobalVector("_Direction", direction);
            _cmd.SetGlobalTexture("_SourceTex", source);
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
            ExecuteBuffer();
        }
    }
}