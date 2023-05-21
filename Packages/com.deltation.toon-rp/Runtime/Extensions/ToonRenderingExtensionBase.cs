using UnityEngine;

namespace DELTation.ToonRP.Extensions
{
    public abstract class ToonRenderingExtensionBase : IToonRenderingExtension
    {
        public abstract void Render();

        public virtual bool ShouldRender(in ToonRenderingExtensionContext context) => true;

        public virtual void Setup(in ToonRenderingExtensionContext context,
            IToonRenderingExtensionSettingsStorage settingsStorage) { }

        public virtual void Cleanup() { }

        protected static bool IsGameOrSceneView(in ToonRenderingExtensionContext context) =>
            context.Camera.cameraType <= CameraType.SceneView;
    }
}