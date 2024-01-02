using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Extensions
{
    public abstract class ToonRenderingExtensionBase : IToonRenderingExtension
    {
        public abstract void Render();

        public virtual bool ShouldRender(in ToonRenderingExtensionContext context) => true;

        public virtual void Setup(in ToonRenderingExtensionContext context,
            IToonRenderingExtensionSettingsStorage settingsStorage) { }

        public virtual void Cleanup() { }

        public virtual void OnPrePass(PrePassMode prePassMode, ref ScriptableRenderContext context,
            CommandBuffer cmd,
            ref DrawingSettings drawingSettings,
            ref FilteringSettings filteringSettings,
            ref RenderStateBlock renderStateBlock) { }

        public virtual bool InterruptsGeometryRenderPass(in ToonRenderingExtensionContext context) => false;

        public virtual void Dispose() { }

        protected static bool IsGameOrSceneView(in ToonRenderingExtensionContext context) =>
            context.Camera.cameraType <= CameraType.SceneView;
    }
}