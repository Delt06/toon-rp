using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    public sealed class ToonDepthDownsample : IDisposable
    {
        public const string ShaderName = "Hidden/Toon RP/Depth Downsample";
        public const string HighQualityKeyword = "_HIGH_QUALITY";
        private static readonly int ResolutionFactorId = Shader.PropertyToID("_ResolutionFactor");

        private readonly ToonPipelineMaterial _material = new(ShaderName, "Toon RP Depth Downsample");

        public void Dispose()
        {
            _material.Dispose();
        }

        public void Downsample(CommandBuffer cmd, bool highQuality, int resolutionFactor)
        {
            Material material = _material.GetOrCreate();

            using (new ProfilingScope(cmd, NamedProfilingSampler.Get("Downsample Depth")))
            {
                material.SetKeyword(HighQualityKeyword, highQuality);
                if (highQuality)
                {
                    material.SetInteger(ResolutionFactorId, resolutionFactor);
                }

                const bool renderToTexture = true;
                ToonBlitter.Blit(cmd, material, renderToTexture, 0);
            }
        }
    }
}