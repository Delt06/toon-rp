using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    public sealed partial class ToonCameraRenderer
    {
#if UNITY_EDITOR
        partial void PrepareForSceneWindow()
        {
            if (_camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(_camera);
            }
        }

        partial void DrawGizmosPreImageEffects()
        {
            if (!Handles.ShouldRenderGizmos())
            {
                return;
            }

            _context.DrawGizmos(_camera, GizmoSubset.PostImageEffects);
        }

        partial void DrawGizmosPostImageEffects()
        {
            if (!Handles.ShouldRenderGizmos())
            {
                return;
            }

            _context.DrawGizmos(_camera, GizmoSubset.PostImageEffects);
        }

#endif // UNITY_EDITOR

#if UNITY_EDITOR || DEBUG
        private static readonly ShaderTagId[] UnsupportedShaderTagIds =
        {
            new("Always"),
            new("ForwardBase"),
            new("PrepassBase"),
            new("Vertex"),
            new("VertexLMRGBM"),
            new("VertexLM"),
        };
        [CanBeNull]
        private static Material _errorMaterial;

        partial void PrepareBufferName()
        {
            _cmdName = _camera.name;
        }

        partial void DrawUnsupportedShaders()
        {
            if (_errorMaterial == null)
            {
                _errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
            }

            var drawingSettings = new DrawingSettings(UnsupportedShaderTagIds[0], new SortingSettings(_camera))
            {
                overrideMaterial = _errorMaterial,
            };

            for (int i = 1; i < UnsupportedShaderTagIds.Length; ++i)
            {
                drawingSettings.SetShaderPassName(i, UnsupportedShaderTagIds[i]);
            }

            FilteringSettings filteringSettings = FilteringSettings.defaultValue;
            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }
#endif // UNITY_EDITOR || DEBUG
    }
}