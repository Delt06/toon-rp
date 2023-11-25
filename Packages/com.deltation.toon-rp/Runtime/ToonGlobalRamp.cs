using System;
using UnityEngine;
using UnityEngine.Rendering;
using static DELTation.ToonRP.ToonRpUtils;

namespace DELTation.ToonRP
{
    public sealed class ToonGlobalRamp
    {
        private const string BufferName = "Global Ramp";
        public const string GlobalRampCrispKeywordName = "_TOON_RP_GLOBAL_RAMP_CRISP";
        public const string GlobalRampTextureKeywordName = "_TOON_RP_GLOBAL_RAMP_TEXTURE";

        private static readonly int GlobalRampId = Shader.PropertyToID("_ToonRP_GlobalRamp");
        private static readonly int GlobalRampSpecularId = Shader.PropertyToID("_ToonRP_GlobalRampSpecular");
        private static readonly int GlobalRampRimId = Shader.PropertyToID("_ToonRP_GlobalRampRim");
        private static readonly int GlobalRampTextureId = Shader.PropertyToID("_ToonRP_GlobalRampTexture");
        private static readonly int AdditionalLightRampOffsetId = Shader.PropertyToID("_AdditionalLightRampOffset");

        private readonly CommandBuffer _cmd = new() { name = BufferName };
        private readonly GlobalKeyword _globalRampCrispKeyword = GlobalKeyword.Create(GlobalRampCrispKeywordName);
        private readonly GlobalKeyword _globalRampTextureKeyword = GlobalKeyword.Create(GlobalRampTextureKeywordName);

        public void Setup(ScriptableRenderContext context, in ToonRampSettings rampSettings)
        {
            _cmd.BeginSample(BufferName);
            SetupGlobalRamp(rampSettings);
            _cmd.EndSample(BufferName);
            context.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
        }

        private void SetupGlobalRamp(in ToonRampSettings rampSettings)
        {
            // diffuse
            {
                float edge1 = rampSettings.Threshold;
                float edge2 = edge1 + rampSettings.Smoothness;
                Vector4 ramp = rampSettings.Mode == ToonGlobalRampMode.CrispAntiAliased
                    ? BuildRampVectorCrispAntiAliased(edge1)
                    : BuildRampVectorFromEdges(edge1, edge2);
                _cmd.SetGlobalVector(GlobalRampId, ramp);
            }

            // specular
            {
                float edge1 = rampSettings.SpecularThreshold;
                float edge2 = edge1 + rampSettings.SpecularSmoothness;
                _cmd.SetGlobalVector(GlobalRampSpecularId, BuildRampVectorFromEdges(edge1, edge2));
            }

            // rim
            {
                float edge1 = rampSettings.RimThreshold;
                float edge2 = edge1 + rampSettings.RimSmoothness;
                _cmd.SetGlobalVector(GlobalRampRimId, BuildRampVectorFromEdges(edge1, edge2));
            }

            // additional lights
            {
                _cmd.SetGlobalVector(AdditionalLightRampOffsetId,
                    new Vector4(
                        rampSettings.AdditionalLights.DiffuseOffset,
                        rampSettings.AdditionalLights.SpecularOffset,
                        rampSettings.AdditionalLights.DistanceAttenuationFactor
                    )
                );
            }

            switch (rampSettings.Mode)
            {
                case ToonGlobalRampMode.Default:
                    _cmd.SetKeyword(_globalRampCrispKeyword, false);
                    _cmd.SetKeyword(_globalRampTextureKeyword, false);
                    break;
                case ToonGlobalRampMode.CrispAntiAliased:
                    _cmd.SetKeyword(_globalRampCrispKeyword, true);
                    _cmd.SetKeyword(_globalRampTextureKeyword, false);
                    break;
                case ToonGlobalRampMode.Texture:
                    _cmd.SetKeyword(_globalRampCrispKeyword, false);
                    _cmd.SetKeyword(_globalRampTextureKeyword, true);
                    _cmd.SetGlobalTexture(GlobalRampTextureId,
                        rampSettings.RampTexture != null ? rampSettings.RampTexture : Texture2D.whiteTexture
                    );
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}