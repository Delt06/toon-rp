using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace DELTation.ToonRP
{
    public static class ToonSceneViewUtils
    {
        public static bool IsDrawingWireframes(Camera camera)
        {
#if UNITY_EDITOR
            foreach (SceneView sceneView in SceneView.sceneViews)
            {
                if (sceneView.camera == camera)
                {
                    return sceneView.cameraMode.drawMode == DrawCameraMode.Wireframe;
                }
            }
#endif // UNITY_EDITOR

            return false;
        }
    }
}