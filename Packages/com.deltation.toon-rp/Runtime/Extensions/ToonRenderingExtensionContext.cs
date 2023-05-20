using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Extensions
{
    public readonly struct ToonRenderingExtensionContext
    {
        public readonly ScriptableRenderContext Context;
        public readonly Camera Camera;

        public ToonRenderingExtensionContext(ScriptableRenderContext context, Camera camera)
        {
            Context = context;
            Camera = camera;
        }
    }
}