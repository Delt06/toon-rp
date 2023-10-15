using DELTation.ToonRP.PostProcessing.BuiltIn;
using UnityEngine;
using UnityEngine.Rendering;

namespace DELTation.ToonRP
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public sealed class ToonAdditionalCameraData : MonoBehaviour, IAdditionalData
    {
        public Camera Camera { get; private set; }

        public Matrix4x4 BaseProjectionMatrix { get; set; }
        public Matrix4x4 JitteredProjectionMatrix { get; set; }
        public Matrix4x4 JitteredGpuProjectionMatrix { get; set; }
        public int RtWidth { get; set; }
        public int RtHeight { get; set; }

        public bool UsingCustomProjection { get; private set; }

        public ToonMotionVectorsPersistentData MotionVectorsPersistentData { get; } = new();
        public ToonTemporalAAPersistentData TemporalAAPersistentData { get; } = new();
        public RTHandleSystem RTHandleSystem { get; } = new();

        private void Awake()
        {
            Camera = GetComponent<Camera>();
        }

        private void OnDestroy()
        {
            TemporalAAPersistentData.Dispose();
            RTHandleSystem.Dispose();
        }

        public void SetCustomProjectionMatrix(Matrix4x4 projectionMatrix)
        {
            Camera.projectionMatrix = projectionMatrix;
            UsingCustomProjection = true;
        }

        public void RestoreProjection()
        {
            if (!UsingCustomProjection)
            {
                return;
            }

            UsingCustomProjection = false;
            Camera.ResetProjectionMatrix();
        }
    }
}