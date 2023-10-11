using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    public class ToonDebugPass : ToonPostProcessingPassBase
    {
        public const string ShaderName = "Hidden/Toon RP/Debug Pass";
        private static readonly int TiledLightingShowTransparentId =
            Shader.PropertyToID("_TiledLighting_ShowTransparent");
        private static readonly int TiledLightingShowOpaqueId =
            Shader.PropertyToID("_TiledLighting_ShowOpaque");
        private static readonly int MotionVectorsScaleId =
            Shader.PropertyToID("_MotionVectors_Scale");
        private static readonly int MotionVectorsSceneIntensityId =
            Shader.PropertyToID("_MotionVectors_SceneIntensity");

        private readonly Material _material = ToonRpUtils.CreateEngineMaterial(ShaderName, "Toon RP Debug Pass");
        private Camera _camera;
        private ToonDebugPassSettings _settings;

        public override bool IsEnabled(in ToonPostProcessingSettings settings) =>
            settings.Find<ToonDebugPassSettings>().IsEffectivelyEnabled();

        public override void Setup(CommandBuffer cmd, in ToonPostProcessingContext context)
        {
            base.Setup(cmd, in context);
            _camera = context.Camera;
            _settings = context.Settings.Find<ToonDebugPassSettings>();
        }

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination)
        {
            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.Debug)))
            {
                if (_camera.cameraType == CameraType.Game)
                {
                    switch (_settings.Mode)
                    {
                        case ToonDebugPassSettings.DebugMode.None:
                            break;
                        case ToonDebugPassSettings.DebugMode.TiledLighting:
                        {
                            _material.SetInt(TiledLightingShowOpaqueId,
                                _settings.TiledLighting.ShowOpaque ? 1 : 0
                            );

                            _material.SetInt(TiledLightingShowTransparentId,
                                _settings.TiledLighting.ShowTransparent ? 1 : 0
                            );
                            break;
                        }
                        case ToonDebugPassSettings.DebugMode.MotionVectors:
                        {
                            _material.SetFloat(MotionVectorsScaleId, _settings.MotionVectors.Scale);
                            _material.SetFloat(MotionVectorsSceneIntensityId, _settings.MotionVectors.SceneIntensity);
                            break;
                        }

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    int passIndex = (int) _settings.Mode - 1;
                    cmd.Blit(source, destination, _material, passIndex);
                }
                else
                {
                    cmd.Blit(source, destination);
                }
            }
        }
    }
}