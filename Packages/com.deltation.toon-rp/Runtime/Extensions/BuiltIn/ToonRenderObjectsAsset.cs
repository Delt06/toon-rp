using System;
using UnityEngine;

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
        }

        public override IToonRenderingExtension CreateExtension() => new ToonRenderObjects();

        protected override string[] ForceIncludedShaderNames() => Array.Empty<string>();
    }
}