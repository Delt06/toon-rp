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
            var msaaField = new PropertyField(msaaProperty);
            var msaaResolveDepth =
                new PropertyField(property.FindPropertyRelative(nameof(ToonCameraRendererSettings.MsaaResolveDepth)));

            void RefreshFields()
            {
                msaaResolveDepth.SetEnabled(msaaProperty.intValue > 1);
            }

            RefreshFields();

            msaaField.RegisterValueChangeCallback(_ => RefreshFields());

            root.Add(msaaField);
            root.Add(msaaResolveDepth);
            root.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonCameraRendererSettings.UseSrpBatching)))
            );
            root.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonCameraRendererSettings.UseDynamicBatching)))
            );

            return root;
        }
    }
}