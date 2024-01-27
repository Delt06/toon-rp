using UnityEngine;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    [CreateAssetMenu(menuName = Path + "SSAO")]
    public class ToonSsaoAsset : ToonRenderingExtensionAsset<ToonSsaoSettings>
    {
        private const ToonRenderingEvent RenderingEvent = ToonRenderingEvent.AfterPrepass;

        private void Reset()
        {
            Settings = new ToonSsaoSettings
            {
                Power = 10.0f,
                Radius = 0.1f,
                KernelSize = 4,
                Threshold = 0.6f,
                Smoothness = 0.2f,
                ResolutionFactor = 2,
                PatternScale = new Vector3(1, 0, 1),
            };
        }

        public override bool IncludesEvent(ToonRenderingEvent renderingEvent) => renderingEvent == RenderingEvent;

        public override IToonRenderingExtension CreateExtensionOrDefault(ToonRenderingEvent renderingEvent) =>
            renderingEvent == RenderingEvent ? new ToonSsao() : null;

        public override PrePassMode RequiredPrePassMode() =>
            PrePassMode.Depth | PrePassMode.Normals;

        protected override string[] ForceIncludedShaderNames() => new[]
        {
            ToonSsao.ShaderName,
        };
    }
}