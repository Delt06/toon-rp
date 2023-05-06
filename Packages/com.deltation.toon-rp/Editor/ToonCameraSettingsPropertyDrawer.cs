using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using static DELTation.ToonRP.ToonCameraRendererSettings;

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

            root.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonCameraRendererSettings.RenderScale))));
            root.Add(new PropertyField(
                    property.FindPropertyRelative(nameof(ToonCameraRendererSettings.MaxRenderTextureWidth))
                )
            );
            root.Add(new PropertyField(
                    property.FindPropertyRelative(nameof(ToonCameraRendererSettings.MaxRenderTextureHeight))
                )
            );
            root.Add(new PropertyField(
                    property.FindPropertyRelative(nameof(ToonCameraRendererSettings.RenderTextureFilterMode))
                )
            );

            root.Add(new IMGUIContainer(() =>
                    {
                        if (property.serializedObject.targetObject is not ToonRenderPipelineAsset pipelineAsset)
                        {
                            return;
                        }

                        DepthPrePassMode mode = pipelineAsset.CameraRendererSettings.DepthPrePass;
                        DepthPrePassMode effectiveMode = pipelineAsset.GetEffectiveDepthPrePassMode();
                        if (mode != effectiveMode)
                        {
                            EditorGUILayout.HelpBox(
                                $"Depth Pre-Pass Mode is overriden by one or many passes to: \"{effectiveMode}\"",
                                MessageType.Warning
                            );
                        }
                    }
                )
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