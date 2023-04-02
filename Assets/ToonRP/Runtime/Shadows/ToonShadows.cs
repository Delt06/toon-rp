using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;

namespace ToonRP.Runtime.Shadows
{
    public sealed class ToonShadows
    {
        private static readonly int ShadowRampId =
            Shader.PropertyToID("_ToonRP_ShadowRamp");
        private readonly CommandBuffer _cmd = new() { name = "Toon Shadows" };
        private ToonBlobShadows _blobShadows;
        private ScriptableRenderContext _context;
        private ToonShadowSettings _settings;
        private ToonVsmShadows _vsmShadows;

        public ToonShadows()
        {
            DirectionalShadowsGlobalKeyword = GlobalKeyword.Create("_TOON_RP_DIRECTIONAL_SHADOWS");
            BlobShadowsGlobalKeyword = GlobalKeyword.Create("_TOON_RP_BLOB_SHADOWS");
            ShadowsRampCrisp = GlobalKeyword.Create("_TOON_RP_SHADOWS_RAMP_CRISP");
        }

        public static GlobalKeyword DirectionalShadowsGlobalKeyword { get; private set; }
        public static GlobalKeyword BlobShadowsGlobalKeyword { get; private set; }
        public static GlobalKeyword ShadowsRampCrisp { get; private set; }

        public void Setup(in ScriptableRenderContext context, in CullingResults cullingResults,
            in ToonShadowSettings settings)
        {
            _context = context;
            _settings = settings;

            if (settings.Mode != ToonShadowSettings.ShadowMode.Vsm)
            {
                _cmd.DisableKeyword(DirectionalShadowsGlobalKeyword);
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
                    _blobShadows.Setup(context, settings);
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