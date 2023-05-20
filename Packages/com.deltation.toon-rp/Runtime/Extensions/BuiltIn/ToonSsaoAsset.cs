using UnityEngine;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    [CreateAssetMenu(menuName = Path + "SSAO")]
    public class ToonSsaoAsset : ToonRenderingExtensionAsset<ToonSsaoSettings>
    {
        public override ToonRenderingEvent Event => ToonRenderingEvent.AfterDepthPrepass;

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

        public override IToonRenderingExtension CreateExtension() => new ToonSsao();

        public override ToonCameraRendererSettings.DepthPrePassMode RequiredDepthPrePassMode() =>
            ToonCameraRendererSettings.DepthPrePassMode.DepthNormals;

        protected override string[] ForceIncludedShaderNames() => new[]
        {
            ToonSsao.ShaderName,
        };
    }
}