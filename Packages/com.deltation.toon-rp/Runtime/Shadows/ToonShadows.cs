﻿using System;
using DELTation.ToonRP.Shadows.Blobs;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Shadows
{
    public sealed class ToonShadows : IDisposable
    {
        public const string DirectionalShadowsKeywordName = "_TOON_RP_DIRECTIONAL_SHADOWS";
        public const string DirectionalCascadedShadowsKeywordName = "_TOON_RP_DIRECTIONAL_CASCADED_SHADOWS";
        public const string VsmKeywordName = "_TOON_RP_VSM";
        public const string PcfKeywordName = "_TOON_RP_PCF";
        public const string PoissonStratifiedKeywordName = "_TOON_RP_POISSON_SAMPLING_STRATIFIED";
        public const string PoissonRotatedKeywordName = "_TOON_RP_POISSON_SAMPLING_ROTATED";
        public const string PoissonEarlyBailKeywordName = "_TOON_RP_POISSON_SAMPLING_EARLY_BAIL";
        public const string BlobShadowsKeywordName = "_TOON_RP_BLOB_SHADOWS";
        public const string ShadowsRampCrispKeywordName = "_TOON_RP_SHADOWS_RAMP_CRISP";
        public const string ShadowsPatternKeywordName = "_TOON_RP_SHADOWS_PATTERN";

        private static readonly int ShadowRampId =
            Shader.PropertyToID("_ToonRP_ShadowRamp");
        private static readonly int ShadowDistanceFadeId =
            Shader.PropertyToID("_ToonRP_ShadowDistanceFade");
        private static readonly int ShadowPatternId =
            Shader.PropertyToID("_ToonRP_ShadowPattern");
        private static readonly int ShadowPatternScaleId =
            Shader.PropertyToID("_ToonRP_ShadowPatternScale");
        private ToonBlobShadows _blobShadows;
        private ScriptableRenderContext _context;
        private ToonShadowSettings _settings;
        private ToonVsmShadows _vsmShadows;

        public ToonShadows()
        {
            DirectionalShadowsGlobalKeyword = GlobalKeyword.Create(DirectionalShadowsKeywordName);
            DirectionalCascadedShadowsGlobalKeyword = GlobalKeyword.Create(DirectionalCascadedShadowsKeywordName);
            VsmGlobalKeyword = GlobalKeyword.Create(VsmKeywordName);
            PcfGlobalKeyword = GlobalKeyword.Create(PcfKeywordName);
            PoissonStratifiedGlobalKeyword = GlobalKeyword.Create(PoissonStratifiedKeywordName);
            PoissonRotatedGlobalKeyword = GlobalKeyword.Create(PoissonRotatedKeywordName);
            PoissonEarlyBailGlobalKeyword = GlobalKeyword.Create(PoissonEarlyBailKeywordName);
            BlobShadowsGlobalKeyword = GlobalKeyword.Create(BlobShadowsKeywordName);
            ShadowsRampCrisp = GlobalKeyword.Create(ShadowsRampCrispKeywordName);
            ShadowsPattern = GlobalKeyword.Create(ShadowsPatternKeywordName);
        }

        public static GlobalKeyword DirectionalShadowsGlobalKeyword { get; private set; }
        public static GlobalKeyword DirectionalCascadedShadowsGlobalKeyword { get; private set; }
        public static GlobalKeyword VsmGlobalKeyword { get; private set; }
        public static GlobalKeyword PcfGlobalKeyword { get; private set; }
        public static GlobalKeyword PoissonStratifiedGlobalKeyword { get; private set; }
        public static GlobalKeyword PoissonRotatedGlobalKeyword { get; private set; }
        public static GlobalKeyword PoissonEarlyBailGlobalKeyword { get; private set; }
        public static GlobalKeyword BlobShadowsGlobalKeyword { get; private set; }
        public static GlobalKeyword ShadowsRampCrisp { get; private set; }
        public static GlobalKeyword ShadowsPattern { get; private set; }

        public void Dispose()
        {
            _blobShadows?.Dispose();
        }

        public void Setup(in ScriptableRenderContext context, in CullingResults cullingResults,
            in ToonShadowSettings settings, Camera camera)
        {
            _context = context;
            _settings = settings;

            CommandBuffer cmd = CommandBufferPool.Get();

            if (settings.Mode != ToonShadowSettings.ShadowMode.Vsm)
            {
                cmd.DisableKeyword(DirectionalShadowsGlobalKeyword);
                cmd.DisableKeyword(DirectionalCascadedShadowsGlobalKeyword);
                cmd.DisableKeyword(VsmGlobalKeyword);
                cmd.DisableKeyword(PcfGlobalKeyword);
                cmd.DisableKeyword(PoissonStratifiedGlobalKeyword);
                cmd.DisableKeyword(PoissonRotatedGlobalKeyword);
                cmd.DisableKeyword(PoissonEarlyBailGlobalKeyword);
            }

            cmd.SetKeyword(BlobShadowsGlobalKeyword, settings.Mode == ToonShadowSettings.ShadowMode.Blobs);

            if (settings.Mode == ToonShadowSettings.ShadowMode.Off)
            {
                cmd.DisableKeyword(ShadowsRampCrisp);
                cmd.DisableKeyword(ShadowsPattern);
            }
            else
            {
                cmd.SetKeyword(ShadowsRampCrisp, _settings.CrispAntiAliased);

                {
                    float effectiveThreshold = 1 - _settings.Threshold;
                    Vector4 ramp = _settings.CrispAntiAliased
                        ? ToonRpUtils.BuildRampVectorCrispAntiAliased(effectiveThreshold)
                        : ToonRpUtils.BuildRampVectorFromSmoothness(effectiveThreshold, _settings.Smoothness);
                    cmd.SetGlobalVector(ShadowRampId,
                        ramp
                    );
                }

                cmd.SetGlobalVector(ShadowDistanceFadeId,
                    new Vector4(
                        1.0f / (_settings.MaxDistance * _settings.MaxDistance),
                        1.0f / _settings.DistanceFade
                    )
                );

                {
                    bool patternEnabled = _settings.Pattern != null;
                    cmd.SetKeyword(ShadowsPattern, patternEnabled);
                    cmd.SetGlobalTexture(ShadowPatternId, patternEnabled ? _settings.Pattern : Texture2D.blackTexture);
                    cmd.SetGlobalVector(ShadowPatternScaleId, _settings.PatternScale);
                }
            }

            _context.ExecuteCommandBufferAndClear(cmd);
            CommandBufferPool.Release(cmd);

            switch (settings.Mode)
            {
                case ToonShadowSettings.ShadowMode.Off:
                    break;
                case ToonShadowSettings.ShadowMode.Vsm:
                    _vsmShadows ??= new ToonVsmShadows();
                    _vsmShadows.Setup(context, cullingResults, settings);
                    break;
                case ToonShadowSettings.ShadowMode.Blobs:
                    _blobShadows ??= new ToonBlobShadows();
                    _blobShadows.Setup(context, settings, camera);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Render([CanBeNull] Light mainLight)
        {
            switch (_settings.Mode)
            {
                case ToonShadowSettings.ShadowMode.Off:
                    break;
                case ToonShadowSettings.ShadowMode.Vsm:
                    if (mainLight != null)
                    {
                        _vsmShadows.ReserveDirectionalShadows(mainLight, 0);
                    }

                    _vsmShadows.Render();
                    break;
                case ToonShadowSettings.ShadowMode.Blobs:
                    _blobShadows.Render();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Cleanup()
        {
            switch (_settings.Mode)
            {
                case ToonShadowSettings.ShadowMode.Off:
                    break;
                case ToonShadowSettings.ShadowMode.Vsm:
                    _vsmShadows.Cleanup();
                    break;
                case ToonShadowSettings.ShadowMode.Blobs:
                    _blobShadows.Cleanup();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}