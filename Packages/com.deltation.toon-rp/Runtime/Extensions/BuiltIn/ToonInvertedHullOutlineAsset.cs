using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    [CreateAssetMenu(menuName = Path + "Inverted Hull Outline")]
    public class
        ToonInvertedHullOutlineAsset : ToonRenderingExtensionAsset<ToonInvertedHullOutlineSettings>
    {
        private const ToonRenderingEvent RenderingEvent = ToonRenderingEvent.AfterOpaque;

        private void Reset()
        {
            Settings = new ToonInvertedHullOutlineSettings
            {
                Passes = new[]
                {
                    new ToonInvertedHullOutlineSettings.Pass
                    {
                        Name = "Outline",
                        Color = Color.black,
                        Thickness = 0.02f,
                        LayerMask = int.MaxValue,
                        StencilPassOp = StencilOp.Keep,
                        MaxDistance = 0.0f,
                        DistanceFade = 0.1f,
                        CameraOverrides = ToonCameraOverrideSettings.Default,
                    },
                },
            };
        }

        public override bool UsesRenderingEvent(ToonRenderingEvent renderingEvent) => renderingEvent == RenderingEvent;

        public override IToonRenderingExtension CreateExtensionOrDefault(ToonRenderingEvent renderingEvent) =>
            renderingEvent == RenderingEvent ? new ToonInvertedHullOutline() : null;


        protected override string[] ForceIncludedShaderNames() => new[]
        {
            ToonInvertedHullOutline.DefaultShaderName,
        };

        public override bool RequiresStencil()
        {
            foreach (ToonInvertedHullOutlineSettings.Pass pass in Settings.Passes)
            {
                if (pass.StencilLayer != StencilLayer.None)
                {
                    return true;
                }
            }

            return false;
        }
    }
}