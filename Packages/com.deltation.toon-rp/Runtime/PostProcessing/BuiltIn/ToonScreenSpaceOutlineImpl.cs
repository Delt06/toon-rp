using UnityEngine;
using UnityEngine.Rendering;
using UnityBlendMode = UnityEngine.Rendering.BlendMode;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    public class ToonScreenSpaceOutlineImpl
    {
        public const string ShaderName = "Hidden/Toon RP/Outline (Screen Space)";
        public const string AlphaBlendingKeywordName = "_ALPHA_BLENDING";
        public const string ColorKeywordName = "_COLOR";
        public const string NormalsKeywordName = "_NORMALS";
        public const string DepthKeywordName = "_DEPTH";
        public const string UseFogKeywordName = "_USE_FOG";

        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int ColorRampId = Shader.PropertyToID("_ColorRamp");
        private static readonly int DepthRampId = Shader.PropertyToID("_DepthRamp");
        private static readonly int NormalsRampId = Shader.PropertyToID("_NormalsRamp");
        private static readonly int DistanceFadeId = Shader.PropertyToID("_DistanceFade");
        private static readonly int BlendSrcId = Shader.PropertyToID("_BlendSrc");
        private static readonly int BlendDstId = Shader.PropertyToID("_BlendDst");
        private static readonly int MainTexTexelSizeId = Shader.PropertyToID("_MainTex_TexelSize");
        private readonly Material _material;

        private readonly Shader _shader;

        public ToonScreenSpaceOutlineImpl()
        {
            _shader = Shader.Find(ShaderName);
            _material = ToonRpUtils.CreateEngineMaterial(_shader, "Toon RP Outline (Screen Space)");
        }

        public void EnableAlphaBlending(bool enable)
        {
            _material.SetKeyword(new LocalKeyword(_shader, AlphaBlendingKeywordName), enable);
            (UnityBlendMode srcBlend, UnityBlendMode dstBlend) =
                enable
                    ? (UnityBlendMode.SrcAlpha, UnityBlendMode.OneMinusSrcAlpha)
                    : (UnityBlendMode.One, UnityBlendMode.Zero);
            _material.SetFloat(BlendSrcId, (float) srcBlend);
            _material.SetFloat(BlendDstId, (float) dstBlend);
        }

        public void SetRtSize(int rtWidth, int rtHeight)
        {
            _material.SetVector(MainTexTexelSizeId, new Vector4(
                    1.0f / rtWidth,
                    1.0f / rtHeight,
                    rtWidth,
                    rtHeight
                )
            );
        }

        public void RenderViaCustomBlit(CommandBuffer cmd, in ToonScreenSpaceOutlineSettings settings)
        {
            UpdateMaterial(settings);

            ToonBlitter.Blit(cmd, _material);
        }

        public void RenderViaBlit(CommandBuffer cmd, in ToonScreenSpaceOutlineSettings settings,
            in RenderTargetIdentifier source, in RenderTargetIdentifier destination)
        {
            UpdateMaterial(settings);

            cmd.Blit(source, destination, _material);
        }

        private void UpdateMaterial(in ToonScreenSpaceOutlineSettings settings)
        {
            _material.SetVector(OutlineColorId, settings.Color.gamma);

            UpdateMaterialFilter(settings.ColorFilter, ColorRampId, ColorKeywordName);
            UpdateMaterialFilter(settings.NormalsFilter, NormalsRampId, NormalsKeywordName);
            UpdateMaterialFilter(settings.DepthFilter, DepthRampId, DepthKeywordName);

            _material.SetKeyword(new LocalKeyword(_shader, UseFogKeywordName), settings.UseFog);

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
            Vector4 ramp = ToonRpUtils.BuildRampVectorFromSmoothness(filter.Threshold, filter.Smoothness);
            _material.SetVector(rampId, ramp);
            _material.SetKeyword(new LocalKeyword(_shader, keyword), filter.Enabled);
        }
    }
}