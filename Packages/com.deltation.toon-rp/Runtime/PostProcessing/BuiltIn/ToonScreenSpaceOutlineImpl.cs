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
        private readonly ToonPipelineMaterial _material;

        private readonly Shader _shader;

        public ToonScreenSpaceOutlineImpl()
        {
            _shader = Shader.Find(ShaderName);
            _material = new ToonPipelineMaterial(_shader, "Toon RP Outline (Screen Space)");
        }

        public void EnableAlphaBlending(bool enable)
        {
            Material material = _material.GetOrCreate();
            material.SetKeyword(new LocalKeyword(_shader, AlphaBlendingKeywordName), enable);
            (UnityBlendMode srcBlend, UnityBlendMode dstBlend) =
                enable
                    ? (UnityBlendMode.SrcAlpha, UnityBlendMode.OneMinusSrcAlpha)
                    : (UnityBlendMode.One, UnityBlendMode.Zero);
            material.SetFloat(BlendSrcId, (float) srcBlend);
            material.SetFloat(BlendDstId, (float) dstBlend);
        }

        public void SetRtSize(int rtWidth, int rtHeight)
        {
            Material material = _material.GetOrCreate();
            material.SetVector(MainTexTexelSizeId, new Vector4(
                    1.0f / rtWidth,
                    1.0f / rtHeight,
                    rtWidth,
                    rtHeight
                )
            );
        }

        public void RenderViaBlit(CommandBuffer cmd, in ToonScreenSpaceOutlineSettings settings,
            bool renderToTexture, RenderTargetIdentifier? source = null)
        {
            UpdateMaterial(settings);

            Material material = _material.GetOrCreate();

            if (source != null)
            {
                cmd.SetGlobalTexture(ToonBlitter.MainTexId, source.Value);
            }

            ToonBlitter.Blit(cmd, material, renderToTexture, 0);
        }

        private void UpdateMaterial(in ToonScreenSpaceOutlineSettings settings)
        {
            Material material = _material.GetOrCreate();
            material.SetVector(OutlineColorId, settings.Color.gamma);

            UpdateMaterialFilter(settings.ColorFilter, ColorRampId, ColorKeywordName);
            UpdateMaterialFilter(settings.NormalsFilter, NormalsRampId, NormalsKeywordName);
            UpdateMaterialFilter(settings.DepthFilter, DepthRampId, DepthKeywordName);

            material.SetKeyword(new LocalKeyword(_shader, UseFogKeywordName), settings.UseFog);

            material.SetVector(DistanceFadeId,
                new Vector4(
                    1.0f / settings.MaxDistance,
                    1.0f / settings.DistanceFade
                )
            );
        }

        private void UpdateMaterialFilter(in ToonScreenSpaceOutlineSettings.OutlineFilter filter, int rampId,
            string keyword)
        {
            Material material = _material.GetOrCreate();
            Vector4 ramp = ToonRpUtils.BuildRampVectorFromSmoothness(filter.Threshold, filter.Smoothness);
            material.SetVector(rampId, ramp);
            material.SetKeyword(new LocalKeyword(_shader, keyword), filter.Enabled);
        }
    }
}