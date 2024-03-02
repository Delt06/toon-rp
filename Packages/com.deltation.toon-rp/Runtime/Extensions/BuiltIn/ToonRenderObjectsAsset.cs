using System;
using UnityEngine;
using UnityEngine.Rendering;
using static DELTation.ToonRP.Extensions.BuiltIn.ToonRenderObjectsSettings.OverrideSettings;

namespace DELTation.ToonRP.Extensions.BuiltIn
{
    [CreateAssetMenu(menuName = Path + "Render Objects")]
    public class ToonRenderObjectsAsset : ToonRenderingExtensionAsset<ToonRenderObjectsSettings>
    {
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

        public override bool UsesRenderingEvent(ToonRenderingEvent renderingEvent) => Settings.Event == renderingEvent;

        public override IToonRenderingExtension CreateExtensionOrDefault(ToonRenderingEvent renderingEvent) =>
            Settings.Event == renderingEvent ? new ToonRenderObjects() : null;

        protected override string[] ForceIncludedShaderNames() => Array.Empty<string>();
    }
}