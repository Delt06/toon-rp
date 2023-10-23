using DELTation.ToonRP.Editor.ShaderGUI.ShaderEnums;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP.Editor.ShaderGUI
{
    [UsedImplicitly]
    public sealed class ToonRpParticlesUnlitShaderGui : ToonRpShaderGuiBase
    {
        private const string SoftParticlesPropertyName = "_SoftParticles";
        private const string SoftParticlesKeyword = ShaderKeywords.SoftParticles;
        private static readonly int SoftParticlesPropertyId = Shader.PropertyToID(SoftParticlesPropertyName);

        protected override void DrawProperties()
        {
            DrawSurfaceProperties(DrawAdditionalSurfaceProperties);

            EditorGUILayout.Space();

            if (DrawFoldout(HeaderNames.Color))
            {
                DrawProperty(PropertyNames.MainColor);
                DrawProperty(PropertyNames.MainTexture);
                DrawProperty(PropertyNames.EmissionColor);
            }
        }

        private void DrawAdditionalSurfaceProperties()
        {
            DrawSoftParticles();
        }

        private void DrawSoftParticles()
        {
            MaterialProperty surfaceTypeProperty = FindProperty(PropertyNames.SurfaceType);
            if (surfaceTypeProperty.hasMixedValue ||
                (SurfaceType) surfaceTypeProperty.floatValue != SurfaceType.Transparent)
            {
                return;
            }

            EditorGUILayout.HelpBox("Soft particles require depth pre-pass.", MessageType.Info);

            if (DrawProperty(SoftParticlesPropertyName))
            {
                UpdateSoftParticles();
            }

            MaterialProperty softParticlesProperty = FindProperty(SoftParticlesPropertyName);
            if (!softParticlesProperty.hasMixedValue && Mathf.Approximately(softParticlesProperty.floatValue, 1.0f))
            {
                DrawProperty("_SoftParticlesDistance");
                DrawProperty("_SoftParticlesRange");
            }
        }

        protected override void OnSurfaceTypeChanged()
        {
            base.OnSurfaceTypeChanged();
            UpdateSoftParticles();
        }

        private void UpdateSoftParticles()
        {
            ForEachMaterial(m =>
                {
                    bool enable = Mathf.Approximately(m.GetFloat(SoftParticlesPropertyId), 1.0f) &&
                                  (SurfaceType) m.GetFloat(PropertyNames.SurfaceType) == SurfaceType.Transparent;
                    m.SetKeyword(SoftParticlesKeyword, enable);
                }
            );
        }

        protected override RenderQueue GetRenderQueue(Material m) => GetRenderQueueWithAlphaTestAndTransparency(m);
    }
}