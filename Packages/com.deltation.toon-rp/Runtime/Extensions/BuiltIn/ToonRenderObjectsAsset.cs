using System;
using UnityEngine;
using UnityEngine.Rendering;
using static DELTation.ToonRP.Extensions.BuiltIn.ToonRenderObjectsSettings.OverrideSettings;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    [CreateAssetMenu(menuName = Path + "Render Objects")]
    public class ToonRenderObjectsAsset : ToonRenderingExtensionAsset<ToonRenderObjectsSettings>
    {
        public override ToonRenderingEvent Event => Settings.Event;

        private void Reset()
        {
            Settings.Event = ToonRenderingEvent.AfterOpaque;
            Settings.Overrides.Camera = ToonCameraOverrideSettings.Default;
            Settings.Overrides.Depth = new DepthOverrideSettings
            {
                DepthTest = CompareFunction.LessEqual,
                WriteDepth = true,
            };
            Settings.Overrides.Stencil = new StencilOverrideSettings
            {
                Value = 0,
                ReadMask = byte.MaxValue,
                WriteMask = byte.MaxValue,
                CompareFunction = CompareFunction.Always,
                Pass = StencilOp.Keep,
                Fail = StencilOp.Keep,
                ZFail = StencilOp.Keep,
            };
        }

        public override IToonRenderingExtension CreateExtension() => new ToonRenderObjects();

        protected override string[] ForceIncludedShaderNames() => Array.Empty<string>();
    }
}