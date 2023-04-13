using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DELTation.ToonRP.Editor
{
    [CustomPropertyDrawer(typeof(ToonCameraRendererSettings))]
    public class ToonCameraSettingsPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            root.Add(new ToonRpHeaderLabel("Camera Renderer"));

            root.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonCameraRendererSettings.AllowHdr)))
                {
                    label = "Allow HDR",
                }
            );
            root.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonCameraRendererSettings.Stencil))));

            root.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonCameraRendererSettings.Msaa)))
                {
                    label = "MSAA",
                }
            );

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