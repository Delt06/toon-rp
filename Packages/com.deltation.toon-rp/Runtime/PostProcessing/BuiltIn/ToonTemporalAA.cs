using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.PostProcessing.BuiltIn
{
    public class ToonTemporalAA : ToonPostProcessingPassBase
    {
        public const string ShaderName = "Hidden/Toon RP/Temporal AA";
        private const string HistoryRtName = "_ToonRP_TAAHistory";
        private static readonly int HistoryRtId = Shader.PropertyToID(HistoryRtName);
        private static readonly int ModulationFactorId = Shader.PropertyToID("_ToonRP_TemporalAA_ModulationFactor");
        private readonly Material _material = ToonRpUtils.CreateEngineMaterial(ShaderName, "Toon RP Temporal AA");
        private ToonTemporalAAPersistentData _persistentData;
        private ToonTemporalAASettings _settings;

        public override void Setup(CommandBuffer cmd, in ToonPostProcessingContext context)
        {
            base.Setup(cmd, in context);

            _persistentData = context.AdditionalCameraData.TemporalAAPersistentData;
            EnsureHistoryIsAllocated(context, _persistentData);

            _settings = context.Settings.Find<ToonTemporalAASettings>();
        }

        private static void EnsureHistoryIsAllocated(ToonPostProcessingContext context,
            ToonTemporalAAPersistentData persistentData)
        {
            if (persistentData.HistoryRt != null)
            {
                return;
            }

            ToonCameraRendererSettings cameraRendererSettings = context.CameraRendererSettings;
            persistentData.HistoryRt = RTHandles.Alloc(Vector2.one,
                colorFormat: ToonCameraRenderer.GetRenderTextureColorFormat(cameraRendererSettings, true),
                filterMode: cameraRendererSettings.RenderTextureFilterMode, name: HistoryRtName
            );
        }

        public override void Render(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination)
        {
            using (new ProfilingScope(cmd, NamedProfilingSampler.Get(ToonRpPassId.TemporalAA)))
            {
                if (_persistentData.HistoryRtStoredValidData)
                {
                    cmd.SetGlobalTexture(HistoryRtId, _persistentData.HistoryRt);
                    cmd.SetGlobalFloat(ModulationFactorId, _settings.ModulationFactor);
                    cmd.Blit(source, destination, _material);
                }
                else
                {
                    cmd.Blit(source, destination);
                }

                cmd.Blit(destination, _persistentData.HistoryRt);
                _persistentData.OnCapturedHistoryRt();
            }
        }
    }
}