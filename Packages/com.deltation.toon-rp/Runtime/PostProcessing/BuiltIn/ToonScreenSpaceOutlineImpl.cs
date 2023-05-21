using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    public class ToonScreenSpaceOutlineImpl
    {
        public const string ShaderName = "Hidden/Toon RP/Outline (Screen Space)";

        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int ColorRampId = Shader.PropertyToID("_ColorRamp");
        private static readonly int DepthRampId = Shader.PropertyToID("_DepthRamp");
        private static readonly int NormalsRampId = Shader.PropertyToID("_NormalsRamp");
        private static readonly int DistanceFadeId = Shader.PropertyToID("_DistanceFade");
        private static readonly int BlendSrcId = Shader.PropertyToID("_BlendSrc");
        private static readonly int BlendDstId = Shader.PropertyToID("_BlendDst");
        private static readonly int MainTexTexelSizeId = Shader.PropertyToID("_MainTex_TexelSize");

        private Material _material;
        private Shader _shader;

        private Material Material
        {
            get
            {
                EnsureMaterialIsCreated();
                return _material;
            }
        }

        public void EnableAlphaBlending(bool enable)
        {
            Material.SetKeyword(new LocalKeyword(_shader, "_ALPHA_BLENDING"), enable);
            (BlendMode srcBlend, BlendMode dstBlend) =
                enable ? (BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha) : (BlendMode.One, BlendMode.Zero);
            Material.SetFloat(BlendSrcId, (float) srcBlend);
            Material.SetFloat(BlendDstId, (float) dstBlend);
        }

        public void SetRtSize(int rtWidth, int rtHeight)
        {
            Material.SetVector(MainTexTexelSizeId, new Vector4(
                    1.0f / rtWidth,
                    1.0f / rtHeight,
                    rtWidth,
                    rtHeight
                )
            );
        }

        private void EnsureMaterialIsCreated()
        {
            if (_material != null && _shader != null)
            {
                return;
            }

            _shader = Shader.Find(ShaderName);
            _material = new Material(_shader)
            {
                name = "Toon RP Outline (Screen Space)",
            };
        }

        public void RenderViaCustomBlit(CommandBuffer cmd, in ToonScreenSpaceOutlineSettings settings)
        {
            EnsureMaterialIsCreated();
            UpdateMaterial(settings);

            CustomBlitter.Blit(cmd, _material);
        }

        public void RenderViaBlit(CommandBuffer cmd, in ToonScreenSpaceOutlineSettings settings,
            in RenderTargetIdentifier source, in RenderTargetIdentifier destination)
        {
            EnsureMaterialIsCreated();
            UpdateMaterial(settings);

            cmd.Blit(source, destination, _material);
        }

        private void UpdateMaterial(in ToonScreenSpaceOutlineSettings settings)
        {
            _material.SetVector(OutlineColorId, settings.Color);

            UpdateMaterialFilter(settings.ColorFilter, ColorRampId, "_COLOR");
            UpdateMaterialFilter(settings.NormalsFilter, NormalsRampId, "_NORMALS");
            UpdateMaterialFilter(settings.DepthFilter, DepthRampId, "_DEPTH");

            _material.SetKeyword(new LocalKeyword(_shader, "_USE_FOG"), settings.UseFog);

            _material.SetVector(DistanceFadeId,
                new Vector4(
                    1.0f / settings.MaxDistance,
                    1.0f / settings.DistanceFade
                )
            );
        }

        private void UpdateMaterialFilter(in ToonScreenSpaceOutlineSettings.OutlineFilter filter, int rampId,
            string keyword)
        {
            var ramp = new Vector4(filter.Threshold, filter.Threshold + filter.Smoothness);
            _material.SetVector(rampId, ramp);
            _material.SetKeyword(new LocalKeyword(_shader, keyword), filter.Enabled);
        }
    }
}