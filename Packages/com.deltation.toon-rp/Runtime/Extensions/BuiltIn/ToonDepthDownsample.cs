using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    public sealed class ToonDepthDownsample
    {
        public const string ShaderName = "Hidden/Toon RP/Depth Downsample";
        public const string HighQualityKeyword = "_HIGH_QUALITY";
        private static readonly int ResolutionFactorId = Shader.PropertyToID("_ResolutionFactor");

        private Material _material;
        private Shader _shader;

        private void EnsureMaterialIsCreated()
        {
            if (_material != null && _shader != null)
            {
                return;
            }

            _shader = Shader.Find(ShaderName);
            _material = new Material(_shader)
            {
                name = "Toon RP Depth Downsample",
            };
        }

        public void Downsample(CommandBuffer cmd, bool highQuality, int resolutionFactor)
        {
            using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Downsample Depth")))
            {
                EnsureMaterialIsCreated();

                _material.SetKeyword(HighQualityKeyword, highQuality);
                if (highQuality)
                {
                    _material.SetInteger(ResolutionFactorId, resolutionFactor);
                }

                CustomBlitter.Blit(cmd, _material);
            }
        }
    }
}