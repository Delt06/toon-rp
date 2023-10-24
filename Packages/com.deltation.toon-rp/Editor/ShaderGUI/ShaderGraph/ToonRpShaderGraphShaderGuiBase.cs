using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Editor.ShaderGUI.ShaderGraph
{
    public abstract class ToonRpShaderGraphShaderGuiBase : ToonRpShaderGuiBase
    {
        private static readonly int ControlOutlinesStencilLayerId =
            Shader.PropertyToID(PropertyNames.ControlOutlinesStencilLayer);
        private static readonly int RenderQueueId = Shader.PropertyToID(PropertyNames.RenderQueue);

        protected override void DrawProperties()
        {
            DrawShaderGraphProperties(Properties);

            if (DrawFoldout("Built-In"))
            {
                if (IsControlOutlinesStencilLayerEnabled())
                {
                    DrawOutlinesStencilLayer();
                }

                DrawExtraBuiltInProperties();
            }
        }

        private bool IsControlOutlinesStencilLayerEnabled()
        {
            Shader shader = GetFirstMaterial().shader;
            return shader.GetPropertyDefaultFloatValueById(ControlOutlinesStencilLayerId) > 0.5f;
        }

        protected virtual void DrawExtraBuiltInProperties() { }

        protected override RenderQueue GetRenderQueue(Material m)
        {
            const float defaultQueue = (float) RenderQueue.Geometry;
            return (RenderQueue) m.shader.GetPropertyDefaultFloatValueById(RenderQueueId, defaultQueue);
        }

        protected override bool CanUseOutlinesStencilLayer(Material m) => true;
    }
}