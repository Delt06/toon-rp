using ToonRP.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ToonRP.Editor
{
    [CustomPropertyDrawer(typeof(ToonCameraRendererSettings))]
    public class ToonCameraSettingsPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            root.Add(new ToonRpHeaderLabel("Camera Renderer"));

            SerializedProperty msaaProperty = property.FindPropertyRelative(nameof(ToonCameraRendererSettings.Msaa));
            var msaaField = new PropertyField(msaaProperty) { label = "MSAA" };
            var msaaResolveDepth =
                new PropertyField(property.FindPropertyRelative(nameof(ToonCameraRendererSettings.MsaaResolveDepth)))
                {
                    label = "MSAA Resolve Depth",
                };

            void RefreshFields()
            {
                msaaResolveDepth.SetEnabled(msaaProperty.intValue > 1);
            }

            RefreshFields();

            msaaField.RegisterValueChangeCallback(_ => RefreshFields());

            root.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonCameraRendererSettings.AllowHdr)))
                {
                    label = "Allow HDR",
                }
            );


            root.Add(msaaField);
            root.Add(msaaResolveDepth);

            root.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonCameraRendererSettings.DepthPrePass)))
                {
                    label = "Depth Pre-Pass Mode",
                }
            );

            root.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonCameraRendererSettings.UseSrpBatching)))
                {
                    label = "Use SRP Batching",
                }
            );
            root.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonCameraRendererSettings.UseDynamicBatching)))
            );

            return root;
        }
    }
}