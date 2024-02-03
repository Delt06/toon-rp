using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    public class ToonDebugPass : ToonPostProcessingPassBase
    {
        public const string ShaderName = "Hidden/Toon RP/Debug Pass";
        private static readonly int MotionVectorsScaleId =
            Shader.PropertyToID("_MotionVectors_Scale");
        private static readonly int MotionVectorsSceneIntensityId =
            Shader.PropertyToID("_MotionVectors_SceneIntensity");

        private readonly ToonPipelineMaterial _material = new(ShaderName, "Toon RP Debug Pass");
        private Camera _camera;
        private ToonDebugPassSettings _settings;

        public override void Dispose()
        {
            base.Dispose();
            _material.Dispose();
        }

        public override bool IsEnabled(in ToonPostProcessingSettings settings) =>
            settings.Find<ToonDebugPassSettings>().IsEffectivelyEnabled();

        public override void Setup(CommandBuffer cmd, in ToonPostProcessingContext context)
        {
            base.Setup(cmd, in context);
            _camera = context.Camera;
            _settings = context.Settings.Find<ToonDebugPassSettings>();
        }

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination, bool destinationIsIntermediateTexture)
        {
            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.Debug)))
            {
                cmd.SetRenderTarget(ToonRpUtils.FixupTextureArrayIdentifier(destination),
                    RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store
                );

                if (_camera.cameraType == CameraType.Game)
                {
                    Material material = _material.GetOrCreate();

                    switch (_settings.Mode)
                    {
                        case ToonDebugPassSettings.DebugMode.None:
                            break;
                        case ToonDebugPassSettings.DebugMode.TiledLighting:
                        {
                            break;
                        }
                        case ToonDebugPassSettings.DebugMode.MotionVectors:
                        {
                            material.SetFloat(MotionVectorsScaleId, _settings.MotionVectors.Scale);
                            material.SetFloat(MotionVectorsSceneIntensityId, _settings.MotionVectors.SceneIntensity);
                            break;
                        }

                        case ToonDebugPassSettings.DebugMode.Depth:
                            break;
                        case ToonDebugPassSettings.DebugMode.Normals:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    int passIndex = (int) _settings.Mode - 1;
                    cmd.SetGlobalTexture(ToonBlitter.MainTexId, ToonRpUtils.FixupTextureArrayIdentifier(source));
                    ToonBlitter.Blit(cmd, material, destinationIsIntermediateTexture, passIndex);
                }
                else
                {
                    ToonBlitter.BlitDefault(cmd, ToonRpUtils.FixupTextureArrayIdentifier(source), destinationIsIntermediateTexture);
                }
            }
        }
    }
}