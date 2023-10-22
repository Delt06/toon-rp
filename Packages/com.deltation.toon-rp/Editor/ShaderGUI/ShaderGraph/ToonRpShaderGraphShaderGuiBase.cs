using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Editor.ShaderGUI.ShaderGraph
{
    public abstract class ToonRpShaderGraphShaderGuiBase : ToonRpShaderGuiBase
    {
        private static readonly int ControlOutlinesStencilLayerId = Shader.PropertyToID("_ControlOutlinesStencilLayer");
        protected override bool ControlQueue => false;

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
            int propertyIndex = GetPropertyIndex(shader, ControlOutlinesStencilLayerId);
            if (propertyIndex == -1)
            {
                return false;
            }

            return shader.GetPropertyDefaultFloatValue(propertyIndex) > 0.5f;
        }

        private static int GetPropertyIndex(Shader shader, int propertyNameId)
        {
            int propertyCount = shader.GetPropertyCount();

            for (int i = 0; i < propertyCount; i++)
            {
                if (shader.GetPropertyNameId(i) == propertyNameId)
                {
                    return i;
                }
            }

            return -1;
        }

        protected virtual void DrawExtraBuiltInProperties() { }

        protected override RenderQueue GetRenderQueue(Material m) => default;

        protected override bool CanUseOutlinesStencilLayer(Material m) => true;
    }
}