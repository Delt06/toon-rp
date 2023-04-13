using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Shadows
{
    public sealed class ToonShadows
    {
        private static readonly int ShadowRampId =
            Shader.PropertyToID("_ToonRP_ShadowRamp");
        private static readonly int ShadowDistanceFadeId =
            Shader.PropertyToID("_ToonRP_ShadowDistanceFade");
        private readonly CommandBuffer _cmd = new() { name = "Toon Shadows" };
        private ToonBlobShadows _blobShadows;
        private ScriptableRenderContext _context;
        private ToonShadowSettings _settings;
        private ToonVsmShadows _vsmShadows;

        public ToonShadows()
        {
            DirectionalShadowsGlobalKeyword = GlobalKeyword.Create("_TOON_RP_DIRECTIONAL_SHADOWS");
            DirectionalCascadedShadowsGlobalKeyword = GlobalKeyword.Create("_TOON_RP_DIRECTIONAL_CASCADED_SHADOWS");
            VsmGlobalKeyword = GlobalKeyword.Create("_TOON_RP_VSM");
            BlobShadowsGlobalKeyword = GlobalKeyword.Create("_TOON_RP_BLOB_SHADOWS");
            ShadowsRampCrisp = GlobalKeyword.Create("_TOON_RP_SHADOWS_RAMP_CRISP");
        }

        public static GlobalKeyword DirectionalShadowsGlobalKeyword { get; private set; }
        public static GlobalKeyword DirectionalCascadedShadowsGlobalKeyword { get; private set; }
        public static GlobalKeyword VsmGlobalKeyword { get; private set; }
        public static GlobalKeyword BlobShadowsGlobalKeyword { get; private set; }
        public static GlobalKeyword ShadowsRampCrisp { get; private set; }

        public void Setup(in ScriptableRenderContext context, in CullingResults cullingResults,
            in ToonShadowSettings settings, Camera camera)
        {
            _context = context;
            _settings = settings;

            if (settings.Mode != ToonShadowSettings.ShadowMode.Vsm)
            {
                _cmd.DisableKeyword(DirectionalShadowsGlobalKeyword);
                _cmd.DisableKeyword(DirectionalCascadedShadowsGlobalKeyword);
                _cmd.DisableKeyword(VsmGlobalKeyword);
            }

            _cmd.SetKeyword(BlobShadowsGlobalKeyword, settings.Mode == ToonShadowSettings.ShadowMode.Blobs);

            if (settings.Mode == ToonShadowSettings.ShadowMode.Off)
            {
                _cmd.DisableKeyword(ShadowsRampCrisp);
            }
            else
            {
                _cmd.SetKeyword(ShadowsRampCrisp, _settings.CrispAntiAliased);

                _cmd.SetGlobalVector(ShadowRampId,
                    new Vector4(
                        _settings.Threshold,
                        _settings.Threshold + _settings.Smoothness
                    )
                );

                _cmd.SetGlobalVector(ShadowDistanceFadeId,
                    new Vector4(
                        1.0f / _settings.MaxDistance,
                        1.0f / _settings.DistanceFade
                    )
                );
            }

            ExecuteBuffer();

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

        private void ExecuteBuffer()
        {
            _context.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
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