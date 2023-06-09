using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    public sealed class ToonDepthDownsample
    {
        public const string ShaderName = "Hidden/Toon RP/Depth Downsample";
        public const string HighQualityKeyword = "_HIGH_QUALITY";
        private static readonly int ResolutionFactorId = Shader.PropertyToID("_ResolutionFactor");

        private readonly Material _material = ToonRpUtils.CreateEngineMaterial(ShaderName, "Toon RP Depth Downsample");

        public void Downsample(CommandBuffer cmd, bool highQuality, int resolutionFactor)
        {
            using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Downsample Depth")))
            {
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