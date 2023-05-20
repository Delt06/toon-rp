using UnityEngine;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    [CreateAssetMenu(menuName = Path + "Inverted Hull Outline")]
    public class
        ToonInvertedHullOutlineAsset : ToonRenderingExtensionAsset<ToonInvertedHullOutlineSettings>
    {
        public override ToonRenderingEvent Event => ToonRenderingEvent.AfterOpaque;

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
                        MaxDistance = 0.0f,
                        DistanceFade = 0.1f,
                    },
                },
            };
        }

        public override IToonRenderingExtension CreateExtension() => new ToonInvertedHullOutline();

        protected override string[] ForceIncludedShaderNames() => new[]
        {
            ToonInvertedHullOutline.ShaderName,
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