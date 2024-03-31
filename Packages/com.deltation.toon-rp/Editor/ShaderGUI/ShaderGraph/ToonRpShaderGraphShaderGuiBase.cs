using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Editor.ShaderGUI.ShaderGraph
{
    public enum QueueControl
    {
        Auto,
        Manual,
    }

    public abstract class ToonRpShaderGraphShaderGuiBase : ToonRpShaderGuiBase
    {
        private static readonly int ControlStencilId = Shader.PropertyToID(PropertyNames.ControlStencil);
        private static readonly int RenderQueueId = Shader.PropertyToID(PropertyNames.RenderQueue);
        private static readonly int QueueControlId = Shader.PropertyToID(PropertyNames.QueueControl);

        protected override void DrawProperties()
        {
            DrawShaderGraphProperties(Properties);

            if (DrawFoldout("Built-In"))
            {
                if (IsControlStencilEnabled())
                {
                    DrawStencilProperties();
                }

                DrawExtraBuiltInProperties();
            }
        }

        private bool IsControlStencilEnabled()
        {
            Shader shader = GetFirstMaterial().shader;
            return shader.GetPropertyDefaultFloatValueById(ControlStencilId) > 0.5f;
        }

        protected virtual void DrawExtraBuiltInProperties() { }

        protected override RenderQueue GetRenderQueue(Material m)
        {
            var queueControl = (QueueControl) m.GetFloat(QueueControlId);
            if (queueControl == QueueControl.Auto)
            {
                return (RenderQueue) m.shader.GetPropertyDefaultFloatValueById(RenderQueueId,
                    (float) RenderQueue.Geometry
                );
            }

            return (RenderQueue) m.GetFloat(RenderQueueId);
        }

        protected override bool CanControlStencil(Material m) => true;

        protected override void DrawQueue()
        {
            DrawQueueControl();
            base.DrawQueue();
        }

        private void DrawQueueControl()
        {
            DrawProperty(PropertyNames.QueueControl, out MaterialProperty queueControlProperty);
            if (queueControlProperty.hasMixedValue)
            {
                return;
            }

            bool manual = (QueueControl) queueControlProperty.floatValue == QueueControl.Manual;
            if (!manual)
            {
                return;
            }

            DrawProperty(PropertyNames.RenderQueue);
        }
    }
}