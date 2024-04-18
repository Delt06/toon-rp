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

            root.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonCameraRendererSettings.AdditionalLights)
                    )
                )
            );

            root.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonCameraRendererSettings.TiledLighting)
                    )
                )
            );

            root.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonCameraRendererSettings.MaxLightsPerTile)
                    )
                )
            );

            root.Add(new PropertyField(
                    property.FindPropertyRelative(nameof(ToonCameraRendererSettings.OverrideRenderTextureFormat))
                )
            );
            root.Add(new PropertyField(
                    property.FindPropertyRelative(nameof(ToonCameraRendererSettings.RenderTextureFormat))
                )
            );

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

                        PrePassMode mode = pipelineAsset.CameraRendererSettings.PrePass;
                        PrePassMode effectiveMode = pipelineAsset.GetEffectiveDepthPrePassMode();
                        if (mode != effectiveMode)
                        {
                            EditorGUILayout.HelpBox(
                                $"Pre-Pass Mode is overriden by one or many passes to: \"{effectiveMode}\"",
                                MessageType.Warning
                            );
                        }
                    }
                )
            );

            root.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonCameraRendererSettings.PrePass)))
                {
                    label = "Pre-Pass Mode",
                }
            );

            root.Add(new PropertyField(property.FindPropertyRelative(nameof(ToonCameraRendererSettings.OpaqueTexture)))
            );

            root.Add(new PropertyField(
                    property.FindPropertyRelative(nameof(ToonCameraRendererSettings.OpaqueLayerMask))
                )
            );
            root.Add(new PropertyField(
                    property.FindPropertyRelative(nameof(ToonCameraRendererSettings.TransparentLayerMask))
                )
            );
            root.Add(new PropertyField(
                    property.FindPropertyRelative(nameof(ToonCameraRendererSettings.MotionVectorsZeroLayerMask))
                )
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

            root.Add(
                new PropertyField(property.FindPropertyRelative(
                        nameof(ToonCameraRendererSettings.ForceRenderToIntermediateBuffer)
                    )
                )
            );

            root.Add(
                new PropertyField(property.FindPropertyRelative(
                        nameof(ToonCameraRendererSettings.NativeRenderPasses)
                    )
                )
                {
                    label = "Native Render Passes (Experimental)",
                }
            );

            root.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonCameraRendererSettings.ForceStoreCameraDepth)
                    )
                )
            );

            root.Add(
                new PropertyField(property.FindPropertyRelative(nameof(ToonCameraRendererSettings.BakedLightingFeatures)
                    )
                )
            );

            return root;
        }
    }
}